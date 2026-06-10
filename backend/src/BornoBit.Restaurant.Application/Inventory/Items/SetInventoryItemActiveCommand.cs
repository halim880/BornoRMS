using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Items;

public record SetInventoryItemActiveCommand(Guid Id, bool IsActive) : IRequest<Unit>;

public class SetInventoryItemActiveCommandValidator : AbstractValidator<SetInventoryItemActiveCommand>
{
    public SetInventoryItemActiveCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class SetInventoryItemActiveCommandHandler : IRequestHandler<SetInventoryItemActiveCommand, Unit>
{
    private readonly IAppDbContext _db;

    public SetInventoryItemActiveCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(SetInventoryItemActiveCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Stock item {request.Id} not found.");

        if (request.IsActive) entity.Activate();
        else entity.Deactivate();

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
