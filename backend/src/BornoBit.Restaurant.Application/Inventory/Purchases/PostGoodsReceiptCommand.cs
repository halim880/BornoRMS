using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Inventory.Consumption;
using BornoBit.Restaurant.Application.Inventory.Payables;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = MediatR.Unit;

namespace BornoBit.Restaurant.Application.Inventory.Purchases;

/// <summary>
/// Post a Draft goods receipt: raise stock for each line (moving-average cost) and write PurchaseIn
/// movements. With <paramref name="PaymentCashAccountId"/> set, also pays the supplier the full receipt
/// value from that account in the same transaction (books a "Purchases" expense) — the cash-at-receipt
/// case. Left null, the receipt becomes an outstanding payable (see GetPayablesQuery).
/// </summary>
public record PostGoodsReceiptCommand(Guid Id, Guid? PaymentCashAccountId = null) : IRequest<Unit>;

public class PostGoodsReceiptCommandValidator : AbstractValidator<PostGoodsReceiptCommand>
{
    public PostGoodsReceiptCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class PostGoodsReceiptCommandHandler : IRequestHandler<PostGoodsReceiptCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly ITransactionNumberGenerator _numbers;
    private readonly ICurrentUser _currentUser;

    public PostGoodsReceiptCommandHandler(
        IAppDbContext db, TimeProvider timeProvider, ITransactionNumberGenerator numbers, ICurrentUser currentUser)
    {
        _db = db;
        _timeProvider = timeProvider;
        _numbers = numbers;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(PostGoodsReceiptCommand request, CancellationToken cancellationToken)
    {
        var grn = await _db.GoodsReceipts
            .Include(g => g.Lines)
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Goods receipt {request.Id} not found.");

        if (grn.Status == GoodsReceiptStatus.Posted)
            throw new ValidationException($"Goods receipt '{grn.GrnNumber}' is already posted.");

        var itemIds = grn.Lines.Select(l => l.InventoryItemId).Distinct().ToList();
        var items = await _db.InventoryItems
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var line in grn.Lines)
        {
            if (!items.TryGetValue(line.InventoryItemId, out var item))
                throw new NotFoundException($"Stock item {line.InventoryItemId} not found.");

            item.Receive(line.QtyBase, line.UnitCost);

            _db.StockMovements.Add(StockMovement.Create(
                item.Id,
                StockMovementType.PurchaseIn,
                qtyBase: line.QtyBase,
                occurredAtUtc: grn.ReceivedAtUtc,
                unitCost: line.UnitCost,
                referenceType: nameof(GoodsReceipt),
                referenceId: grn.Id));

            await StockProjectionWriter.BumpAsync(_db, item.Id, item.QtyOnHand, nowUtc, cancellationToken);
        }

        grn.MarkPosted(nowUtc);

        // PO/GRN matching: bump the received tally on each matched purchase-order line, then advance PO status.
        if (grn.PurchaseOrderId is { } poId)
        {
            var po = await _db.PurchaseOrders
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == poId, cancellationToken)
                ?? throw new NotFoundException($"Purchase order {poId} not found.");

            foreach (var line in grn.Lines)
            {
                if (line.PurchaseOrderLineId is { } poLineId)
                    po.ApplyReceipt(poLineId, line.QtyBase);
            }
            po.RecomputeStatus();
        }

        // Optional cash-at-receipt: settle the full value to the supplier now, booking the Purchases expense.
        if (request.PaymentCashAccountId is { } accountId)
        {
            await SupplierPaymentPoster.AddAsync(
                _db, _numbers, _currentUser, nowUtc,
                grn.SupplierId, accountId, grn.ReceivedAtUtc, grn.Subtotal,
                reference: grn.GrnNumber, notes: $"Payment on goods receipt {grn.GrnNumber}", cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
