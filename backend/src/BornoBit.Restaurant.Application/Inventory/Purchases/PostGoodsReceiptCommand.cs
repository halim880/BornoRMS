using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Inventory.Consumption;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = MediatR.Unit;

namespace BornoBit.Restaurant.Application.Inventory.Purchases;

/// <summary>Post a Draft goods receipt: raise stock for each line (moving-average cost) and write PurchaseIn movements.</summary>
public record PostGoodsReceiptCommand(Guid Id) : IRequest<Unit>;

public class PostGoodsReceiptCommandValidator : AbstractValidator<PostGoodsReceiptCommand>
{
    public PostGoodsReceiptCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class PostGoodsReceiptCommandHandler : IRequestHandler<PostGoodsReceiptCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public PostGoodsReceiptCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
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
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
