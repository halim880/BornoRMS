using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Categories;

public record UpdateInventoryCategoryCommand(
    Guid Id,
    string Name,
    string? BanglaName,
    string? Description,
    int DisplayOrder
) : IRequest<Unit>;

public class UpdateInventoryCategoryCommandValidator : AbstractValidator<UpdateInventoryCategoryCommand>
{
    public UpdateInventoryCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BanglaName).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateInventoryCategoryCommandHandler : IRequestHandler<UpdateInventoryCategoryCommand, Unit>
{
    private readonly IAppDbContext _db;

    public UpdateInventoryCategoryCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateInventoryCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.InventoryCategories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Inventory category {request.Id} not found.");

        entity.UpdateDetails(request.Name, request.DisplayOrder, request.BanglaName, request.Description);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
