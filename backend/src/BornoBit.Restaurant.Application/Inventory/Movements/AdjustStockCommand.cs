using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Inventory.Consumption;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = MediatR.Unit;

namespace BornoBit.Restaurant.Application.Inventory.Movements;

/// <summary>Reconcile an item to a physical count. <paramref name="CountedQtyBase"/> is in the item's base unit.</summary>
public record AdjustStockCommand(Guid ItemId, decimal CountedQtyBase, string? Reason) : IRequest<Unit>;

public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.CountedQtyBase).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public AdjustStockCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var item = await _db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken)
            ?? throw new NotFoundException($"Stock item {request.ItemId} not found.");

        var delta = item.AdjustTo(request.CountedQtyBase);
        if (delta == 0)
        {
            // No change in on-hand quantity; nothing to record.
            return Unit.Value;
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var movement = StockMovement.Create(
            item.Id,
            delta > 0 ? StockMovementType.AdjustmentIn : StockMovementType.AdjustmentOut,
            qtyBase: delta,
            occurredAtUtc: nowUtc,
            unitCost: item.AvgCost,
            reason: request.Reason ?? "Physical count adjustment");

        _db.StockMovements.Add(movement);
        await StockProjectionWriter.BumpAsync(_db, item.Id, item.QtyOnHand, nowUtc, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
