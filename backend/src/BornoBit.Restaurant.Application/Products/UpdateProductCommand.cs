using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Products;

public record UpdateProductCommand(
    Guid Id,
    Guid ProductCategoryId,
    string Code,
    string Name,
    string? BanglaName,
    decimal Price,
    string? Description,
    string? ImagePath,
    int DisplayOrder,
    IReadOnlyList<ProductVariantInput>? Variants = null
) : IRequest<Unit>;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
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

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Unit>
{
    private readonly IAppDbContext _db;

    public UpdateProductCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Product {request.Id} not found.");

        var categoryExists = await _db.ProductCategories
            .AnyAsync(c => c.Id == request.ProductCategoryId, cancellationToken);
        if (!categoryExists) throw new NotFoundException($"Product category {request.ProductCategoryId} not found.");

        var code = request.Code.Trim().ToUpperInvariant();
        var clash = await _db.Products.AnyAsync(p => p.Id != request.Id && p.Code == code, cancellationToken);
        if (clash) throw new ValidationException($"A product with code '{code}' already exists.");

        entity.UpdateDetails(
            request.ProductCategoryId,
            code,
            request.Name,
            request.Price,
            request.BanglaName,
            request.ImagePath,
            request.Description,
            request.DisplayOrder);

        if (request.Variants is not null)
        {
            var existingIds = entity.Variants.Select(v => v.Id).ToHashSet();
            entity.SyncVariants(request.Variants.Select(v => (v.Id, v.Name, v.Price, v.DisplayOrder)).ToList());
            // The product is tracked, so new variants (pre-set Guid keys) would be discovered as
            // Modified and saved as UPDATEs — mark them Added explicitly.
            foreach (var v in entity.Variants.Where(v => !existingIds.Contains(v.Id)))
                _db.ProductVariants.Add(v);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
