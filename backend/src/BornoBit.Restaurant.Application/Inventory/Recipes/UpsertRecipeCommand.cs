using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Catalog;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Recipes;

/// <summary>
/// Creates or updates a product's recipe (BOM) and flips the product to <see cref="InventoryMethod.RecipeBased"/>.
/// Returns the recipe id.
/// </summary>
public record UpsertRecipeCommand(Guid ProductId, Guid? VariantId, decimal Yield, IReadOnlyList<RecipeItemInput> Items)
    : IRequest<Guid>;

public class UpsertRecipeCommandValidator : AbstractValidator<UpsertRecipeCommand>
{
    public UpsertRecipeCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Yield).GreaterThan(0).WithMessage("Yield must be greater than zero.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("A recipe needs at least one ingredient.");
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(r => r.InventoryItemId).NotEmpty().WithMessage("Ingredient is required.");
            i.RuleFor(r => r.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
            i.RuleFor(r => r.UnitId).NotEmpty().WithMessage("Unit is required.");
        });
        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.InventoryItemId).Distinct().Count() == items.Count)
            .WithMessage("An ingredient cannot appear twice in the same recipe.");
    }
}

public class UpsertRecipeCommandHandler : IRequestHandler<UpsertRecipeCommand, Guid>
{
    private readonly IAppDbContext _db;

    public UpsertRecipeCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(UpsertRecipeCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException($"Product {request.ProductId} not found.");

        // Validate referenced ingredients + units exist.
        var itemIds = request.Items.Select(i => i.InventoryItemId).Distinct().ToList();
        var knownItems = await _db.InventoryItems.Where(i => itemIds.Contains(i.Id)).Select(i => i.Id).ToListAsync(cancellationToken);
        if (knownItems.Count != itemIds.Count) throw new ValidationException("One or more ingredients no longer exist.");

        var unitIds = request.Items.Select(i => i.UnitId).Distinct().ToList();
        var knownUnits = await _db.Units.Where(u => unitIds.Contains(u.Id)).Select(u => u.Id).ToListAsync(cancellationToken);
        if (knownUnits.Count != unitIds.Count) throw new ValidationException("One or more units no longer exist.");

        var recipe = await _db.Recipes
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.ProductId == request.ProductId && r.VariantId == request.VariantId, cancellationToken);

        if (recipe is null)
        {
            recipe = Recipe.Create(request.ProductId, request.VariantId, request.Yield);
            _db.Recipes.Add(recipe);
        }
        else
        {
            recipe.SetYield(request.Yield);
            recipe.Activate();
        }

        recipe.SyncItems(request.Items
            .Select(i => (i.Id, i.InventoryItemId, i.Quantity, i.UnitId))
            .ToList());

        product.SetInventoryMethod(InventoryMethod.RecipeBased);

        await _db.SaveChangesAsync(cancellationToken);
        return recipe.Id;
    }
}
