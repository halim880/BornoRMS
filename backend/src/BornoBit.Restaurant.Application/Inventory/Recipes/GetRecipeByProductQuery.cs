using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Recipes;

/// <summary>Returns the recipe (BOM) for a product, or null if none is defined yet.</summary>
public record GetRecipeByProductQuery(Guid ProductId, Guid? VariantId = null) : IRequest<RecipeDto?>;

public class GetRecipeByProductQueryHandler : IRequestHandler<GetRecipeByProductQuery, RecipeDto?>
{
    private readonly IAppDbContext _db;

    public GetRecipeByProductQueryHandler(IAppDbContext db) => _db = db;

    public async Task<RecipeDto?> Handle(GetRecipeByProductQuery request, CancellationToken cancellationToken)
    {
        var recipe = await _db.Recipes
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.ProductId == request.ProductId && r.VariantId == request.VariantId, cancellationToken);
        if (recipe is null) return null;

        var productName = await _db.Products.Where(p => p.Id == recipe.ProductId).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken) ?? "";

        var itemIds = recipe.Items.Select(i => i.InventoryItemId).ToList();
        var unitIds = recipe.Items.Select(i => i.UnitId).ToList();
        var items = await _db.InventoryItems.Where(i => itemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Code, i.Name }).ToDictionaryAsync(i => i.Id, cancellationToken);
        var units = await _db.Units.Where(u => unitIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Code }).ToDictionaryAsync(u => u.Id, cancellationToken);

        var rows = recipe.Items.Select(ri => new RecipeItemDto(
            ri.Id, ri.InventoryItemId,
            items.TryGetValue(ri.InventoryItemId, out var it) ? it.Code : "?",
            items.TryGetValue(ri.InventoryItemId, out var it2) ? it2.Name : "?",
            ri.Quantity, ri.UnitId,
            units.TryGetValue(ri.UnitId, out var u) ? u.Code : "?")).ToList();

        return new RecipeDto(recipe.Id, recipe.ProductId, productName, recipe.VariantId, recipe.Yield, recipe.IsActive, rows);
    }
}
