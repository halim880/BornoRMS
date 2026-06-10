using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Inventory;
using FluentValidation;
using MediatR;

namespace BornoBit.Restaurant.Application.Inventory.Categories;

public record CreateInventoryCategoryCommand(
    string Name,
    string? BanglaName,
    string? Description,
    int DisplayOrder
) : IRequest<Guid>;

public class CreateInventoryCategoryCommandValidator : AbstractValidator<CreateInventoryCategoryCommand>
{
    public CreateInventoryCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BanglaName).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class CreateInventoryCategoryCommandHandler : IRequestHandler<CreateInventoryCategoryCommand, Guid>
{
    private readonly IAppDbContext _db;

    public CreateInventoryCategoryCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateInventoryCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = InventoryCategory.Create(request.Name, request.DisplayOrder, request.BanglaName, request.Description);
        _db.InventoryCategories.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
