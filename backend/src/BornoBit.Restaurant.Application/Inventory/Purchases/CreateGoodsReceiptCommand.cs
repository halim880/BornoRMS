using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Purchases;

public record GoodsReceiptLineInput(Guid ItemId, decimal Qty, Guid UnitId, decimal UnitCost, Guid? PurchaseOrderLineId = null);

/// <summary>
/// Create a Draft goods receipt. Stock is not moved until the receipt is posted. When
/// <paramref name="PurchaseOrderId"/> is set the receipt is matched to that PO — posting it bumps the
/// matched PO lines' received quantities and advances the PO status.
/// </summary>
public record CreateGoodsReceiptCommand(
    Guid SupplierId,
    string? InvoiceNo,
    DateTime? ReceivedAtUtc,
    string? Notes,
    IReadOnlyList<GoodsReceiptLineInput> Lines,
    Guid? PurchaseOrderId = null
) : IRequest<Guid>;

public class CreateGoodsReceiptCommandValidator : AbstractValidator<CreateGoodsReceiptCommand>
{
    public CreateGoodsReceiptCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.InvoiceNo).MaximumLength(80);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one line is required.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).NotEmpty();
            line.RuleFor(l => l.UnitId).NotEmpty();
            line.RuleFor(l => l.Qty).GreaterThan(0);
            line.RuleFor(l => l.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}

public class CreateGoodsReceiptCommandHandler : IRequestHandler<CreateGoodsReceiptCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public CreateGoodsReceiptCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreateGoodsReceiptCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.Suppliers.AnyAsync(s => s.Id == request.SupplierId, cancellationToken))
            throw new NotFoundException($"Supplier {request.SupplierId} not found.");

        // When matched to a PO, validate it belongs to the same supplier and is open for receiving.
        if (request.PurchaseOrderId is { } poId)
        {
            var po = await _db.PurchaseOrders
                .FirstOrDefaultAsync(p => p.Id == poId, cancellationToken)
                ?? throw new NotFoundException($"Purchase order {poId} not found.");
            if (po.SupplierId != request.SupplierId)
                throw new ValidationException("The goods receipt supplier must match the purchase order's supplier.");
            if (po.Status is not (Domain.Inventory.PurchaseOrderStatus.Approved or Domain.Inventory.PurchaseOrderStatus.PartiallyReceived))
                throw new ValidationException($"Purchase order '{po.PoNumber}' is {po.Status} — goods can only be received against an approved order.");
        }

        var itemIds = request.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await _db.InventoryItems
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        var unitIds = request.Lines.Select(l => l.UnitId).Distinct().ToList();
        var units = await _db.Units
            .Where(u => unitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var baseUnitIds = items.Values.Select(i => i.BaseUnitId).Distinct().ToList();
        var baseUnits = await _db.Units
            .Where(u => baseUnitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var receivedAt = request.ReceivedAtUtc ?? nowUtc;

        var grn = GoodsReceipt.Create(
            await NextGrnNumberAsync(nowUtc, cancellationToken),
            request.SupplierId,
            receivedAt,
            request.InvoiceNo,
            notes: request.Notes,
            purchaseOrderId: request.PurchaseOrderId);

        foreach (var line in request.Lines)
        {
            if (!items.TryGetValue(line.ItemId, out var item))
                throw new NotFoundException($"Stock item {line.ItemId} not found.");
            if (!units.TryGetValue(line.UnitId, out var unit))
                throw new NotFoundException($"Unit {line.UnitId} not found.");

            if (baseUnits.TryGetValue(item.BaseUnitId, out var baseUnit) && baseUnit.Dimension != unit.Dimension)
                throw new ValidationException($"Unit '{unit.Code}' is not compatible with '{item.Name}' (base unit '{baseUnit.Code}').");

            var qtyBase = unit.ToBase(line.Qty);
            grn.AddLine(item.Id, item.Name, line.Qty, unit.Id, qtyBase, line.UnitCost, line.PurchaseOrderLineId);
        }

        _db.GoodsReceipts.Add(grn);
        await _db.SaveChangesAsync(cancellationToken);
        return grn.Id;
    }

    private async Task<string> NextGrnNumberAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var dayStart = nowUtc.Date;
        var dayEnd = dayStart.AddDays(1);
        var countToday = await _db.GoodsReceipts
            .CountAsync(g => g.ReceivedAtUtc >= dayStart && g.ReceivedAtUtc < dayEnd, cancellationToken);
        return $"GRN-{dayStart:yyyyMMdd}-{countToday + 1:D4}";
    }
}
