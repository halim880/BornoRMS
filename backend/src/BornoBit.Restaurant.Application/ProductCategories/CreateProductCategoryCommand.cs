using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Catalog;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.ProductCategories;

public record CreateProductCategoryCommand(
    string Name,
    string? Description,
    int DisplayOrder,
    decimal? TaxRatePercent = null
) : IRequest<Guid>;

public class CreateProductCategoryCommandValidator : AbstractValidator<CreateProductCategoryCommand>
{
    public CreateProductCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TaxRatePercent).InclusiveBetween(0, 100).When(x => x.TaxRatePercent.HasValue);
    }
}

public class CreateProductCategoryCommandHandler : IRequestHandler<CreateProductCategoryCommand, Guid>
{
    private readonly IAppDbContext _db;

    public CreateProductCategoryCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateProductCategoryCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var clash = await _db.ProductCategories.AnyAsync(c => c.Name == name, cancellationToken);
        if (clash) throw new ValidationException($"A product category named '{name}' already exists.");

        var entity = ProductCategory.Create(name, request.DisplayOrder, request.Description, request.TaxRatePercent);
        _db.ProductCategories.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
