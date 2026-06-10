using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Categories;

public record SetInventoryCategoryActiveCommand(Guid Id, bool IsActive) : IRequest<Unit>;

public class SetInventoryCategoryActiveCommandValidator : AbstractValidator<SetInventoryCategoryActiveCommand>
{
    public SetInventoryCategoryActiveCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class SetInventoryCategoryActiveCommandHandler : IRequestHandler<SetInventoryCategoryActiveCommand, Unit>
{
    private readonly IAppDbContext _db;

    public SetInventoryCategoryActiveCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(SetInventoryCategoryActiveCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.InventoryCategories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Inventory category {request.Id} not found.");

        if (request.IsActive) entity.Activate();
        else entity.Deactivate();

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
