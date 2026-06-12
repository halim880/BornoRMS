using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Catalog;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Products;

/// <summary>Variant row sent from the product form; Id is null for new rows.</summary>
public record ProductVariantInput(Guid? Id, string Name, decimal Price, int DisplayOrder);

public record CreateProductCommand(
    Guid ProductCategoryId,
    string Code,
    string Name,
    string? BanglaName,
    decimal Price,
    string? Description,
    string? ImagePath,
    int DisplayOrder,
    IReadOnlyList<ProductVariantInput>? Variants = null
) : IRequest<Guid>;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.ProductCategoryId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BanglaName).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.ImagePath).MaximumLength(500);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleForEach(x => x.Variants).ChildRules(v =>
        {
            v.RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            v.RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.Variants)
            .Must(v => v is null || v.Select(x => x.Name.Trim().ToLowerInvariant()).Distinct().Count() == v.Count)
            .WithMessage("Variant names must be unique.");
    }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IAppDbContext _db;

    public CreateProductCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var categoryExists = await _db.ProductCategories
            .AnyAsync(c => c.Id == request.ProductCategoryId, cancellationToken);
        if (!categoryExists) throw new NotFoundException($"Product category {request.ProductCategoryId} not found.");

        var code = request.Code.Trim().ToUpperInvariant();
        var clash = await _db.Products.AnyAsync(p => p.Code == code, cancellationToken);
        if (clash) throw new ValidationException($"A product with code '{code}' already exists.");

        var entity = Product.Create(
            request.ProductCategoryId,
            code,
            request.Name,
            request.Price,
            request.BanglaName,
            request.ImagePath,
            request.Description,
            request.DisplayOrder);

        foreach (var variant in request.Variants ?? Array.Empty<ProductVariantInput>())
            entity.AddVariant(variant.Name, variant.Price, variant.DisplayOrder);

        _db.Products.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
