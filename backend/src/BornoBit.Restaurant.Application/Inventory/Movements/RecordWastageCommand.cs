using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = MediatR.Unit;

namespace BornoBit.Restaurant.Application.Inventory.Movements;

/// <summary>Record spoilage/nosto. <paramref name="QtyBase"/> is in the item's base unit. Reason is required.</summary>
public record RecordWastageCommand(Guid ItemId, decimal QtyBase, string Reason) : IRequest<Unit>;

public class RecordWastageCommandValidator : AbstractValidator<RecordWastageCommand>
{
    public RecordWastageCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.QtyBase).GreaterThan(0).WithMessage("Wastage quantity must be greater than zero.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A reason is required for wastage.").MaximumLength(500);
    }
}

public class RecordWastageCommandHandler : IRequestHandler<RecordWastageCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public RecordWastageCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(RecordWastageCommand request, CancellationToken cancellationToken)
    {
        var item = await _db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken)
            ?? throw new NotFoundException($"Stock item {request.ItemId} not found.");

        item.WriteOff(request.QtyBase);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var movement = StockMovement.Create(
            item.Id,
            StockMovementType.WastageOut,
            qtyBase: -request.QtyBase,
            occurredAtUtc: nowUtc,
            unitCost: item.AvgCost,
            reason: request.Reason);

        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
