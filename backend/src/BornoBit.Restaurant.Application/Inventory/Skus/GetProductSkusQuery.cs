using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Catalog;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Skus;

/// <summary>One product/variant "slot" and the stock SKU linked to it (null fields = no SKU yet).</summary>
public record SkuSlotDto(
    Guid? VariantId,
    string? VariantName,
    Guid? ItemId,
    string? ItemCode,
    decimal? QtyOnHand,
    string? UnitCode);

/// <summary>A sellable product with one SKU slot per variant (or a single product-level slot when it has none).</summary>
public record ProductSkusDto(
    Guid ProductId,
    string Code,
    string Name,
    InventoryMethod Method,
    IReadOnlyList<SkuSlotDto> Slots);

/// <summary>
/// Feeds the Product SKUs page: sellable products (non-kitchen — RecipeBased products deduct ingredients,
/// not per-variant SKUs) with the DirectStock item linked to each variant, so coverage shows at a glance.
/// </summary>
public record GetProductSkusQuery : IRequest<IReadOnlyList<ProductSkusDto>>;

public class GetProductSkusQueryHandler : IRequestHandler<GetProductSkusQuery, IReadOnlyList<ProductSkusDto>>
{
    private readonly IAppDbContext _db;

    public GetProductSkusQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductSkusDto>> Handle(GetProductSkusQuery request, CancellationToken cancellationToken)
    {
        var products = await _db.Products
            .Where(p => p.InventoryMethod != InventoryMethod.RecipeBased)
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name)
            .Select(p => new { p.Id, p.Code, p.Name, p.InventoryMethod })
            .ToListAsync(cancellationToken);

        if (products.Count == 0) return Array.Empty<ProductSkusDto>();

        var productIds = products.Select(p => p.Id).ToList();

        var variants = await _db.ProductVariants
            .Where(v => productIds.Contains(v.ProductId))
            .OrderBy(v => v.DisplayOrder)
            .Select(v => new { v.Id, v.ProductId, v.Name })
            .ToListAsync(cancellationToken);

        // Linked SKUs (DirectStock items pointing at these products), joined to their unit code.
        var links = await (
            from i in _db.InventoryItems
            join u in _db.Units on i.BaseUnitId equals u.Id
            where i.ProductId != null && productIds.Contains(i.ProductId!.Value)
            select new { ProductId = i.ProductId!.Value, i.VariantId, ItemId = i.Id, i.Code, i.QtyOnHand, UnitCode = u.Code })
            .ToListAsync(cancellationToken);

        var variantsByProduct = variants.ToLookup(v => v.ProductId);
        var linkByKey = links.ToDictionary(l => (l.ProductId, l.VariantId));

        SkuSlotDto Slot(Guid productId, Guid? variantId, string? variantName)
        {
            return linkByKey.TryGetValue((productId, variantId), out var l)
                ? new SkuSlotDto(variantId, variantName, l.ItemId, l.Code, l.QtyOnHand, l.UnitCode)
                : new SkuSlotDto(variantId, variantName, null, null, null, null);
        }

        var result = new List<ProductSkusDto>(products.Count);
        foreach (var p in products)
        {
            var pv = variantsByProduct[p.Id].ToList();
            var slots = pv.Count > 0
                ? pv.Select(v => Slot(p.Id, v.Id, v.Name)).ToList()
                : new List<SkuSlotDto> { Slot(p.Id, null, null) };

            result.Add(new ProductSkusDto(p.Id, p.Code, p.Name, p.InventoryMethod, slots));
        }

        return result;
    }
}
