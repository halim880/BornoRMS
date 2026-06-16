using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Store;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Store.Items;

// ---- Wastage / spoilage write-off ----

public record WriteOffStoreStockCommand(Guid ItemId, decimal Qty, Guid UnitId, string? Reason) : IRequest<Unit>;

public class WriteOffStoreStockCommandValidator : AbstractValidator<WriteOffStoreStockCommand>
{
    public WriteOffStoreStockCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.Qty).GreaterThan(0);
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class WriteOffStoreStockCommandHandler : IRequestHandler<WriteOffStoreStockCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public WriteOffStoreStockCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(WriteOffStoreStockCommand request, CancellationToken cancellationToken)
    {
        var item = await _db.StoreItems.FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken)
            ?? throw new NotFoundException($"Store item {request.ItemId} not found.");

        var unit = await _db.StoreUnits.FirstOrDefaultAsync(u => u.Id == request.UnitId, cancellationToken)
            ?? throw new NotFoundException($"Store unit {request.UnitId} not found.");

        var baseUnit = await _db.StoreUnits.FirstOrDefaultAsync(u => u.Id == item.BaseUnitId, cancellationToken);
        if (baseUnit is not null && baseUnit.Dimension != unit.Dimension)
            throw new ValidationException($"Unit '{unit.Code}' is not compatible with '{item.Name}' (base unit '{baseUnit.Code}').");

        var qtyBase = unit.ToBase(request.Qty);
        item.WriteOff(qtyBase);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        _db.StoreStockMovements.Add(StoreStockMovement.Create(
            item.Id, StoreMovementType.WastageOut,
            qtyBase: -qtyBase, occurredAtUtc: nowUtc,
            unitCost: item.AvgCost,
            reason: request.Reason ?? "Wastage / spoilage"));

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// ---- Opening balance (one-time seed of on-hand + cost) ----

public record SetStoreOpeningBalanceCommand(Guid ItemId, decimal Qty, Guid UnitId, decimal UnitCost, DateTime? AsOfUtc) : IRequest<Unit>;

public class SetStoreOpeningBalanceCommandValidator : AbstractValidator<SetStoreOpeningBalanceCommand>
{
    public SetStoreOpeningBalanceCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.Qty).GreaterThan(0);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
    }
}

public class SetStoreOpeningBalanceCommandHandler : IRequestHandler<SetStoreOpeningBalanceCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public SetStoreOpeningBalanceCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(SetStoreOpeningBalanceCommand request, CancellationToken cancellationToken)
    {
        var item = await _db.StoreItems.FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken)
            ?? throw new NotFoundException($"Store item {request.ItemId} not found.");

        // Opening balance is a one-time seed; once stock exists, corrections go through Adjust.
        if (item.QtyOnHand != 0)
            throw new ValidationException($"'{item.Name}' already has stock on hand; use Adjust to correct the count.");

        var unit = await _db.StoreUnits.FirstOrDefaultAsync(u => u.Id == request.UnitId, cancellationToken)
            ?? throw new NotFoundException($"Store unit {request.UnitId} not found.");

        var baseUnit = await _db.StoreUnits.FirstOrDefaultAsync(u => u.Id == item.BaseUnitId, cancellationToken);
        if (baseUnit is not null && baseUnit.Dimension != unit.Dimension)
            throw new ValidationException($"Unit '{unit.Code}' is not compatible with '{item.Name}' (base unit '{baseUnit.Code}').");

        var qtyBase = unit.ToBase(request.Qty);
        item.Receive(qtyBase, request.UnitCost);

        var occurredAt = request.AsOfUtc ?? _timeProvider.GetUtcNow().UtcDateTime;
        _db.StoreStockMovements.Add(StoreStockMovement.Create(
            item.Id, StoreMovementType.OpeningBalance,
            qtyBase: qtyBase, occurredAtUtc: occurredAt,
            unitCost: request.UnitCost,
            reason: "Opening balance"));

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
