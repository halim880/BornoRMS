using BornoBit.Restaurant.Domain.Catalog;
using BornoBit.Restaurant.Shared.Common;

namespace BornoBit.Restaurant.Application.Ordering.Commands;

/// <summary>
/// Resolves a (product, variant) pair from the waiter flow into the snapshot values an
/// <see cref="Domain.Ordering.OrderLine"/> stores, enforcing the catalog rules:
/// the product must exist and be active; a product with active variants requires one of them;
/// a supplied variant must belong to the product and be active.
/// </summary>
internal static class OrderLineResolver
{
    public static (string Name, decimal Price, string Currency, string Code, Guid? StationId) Resolve(
        IReadOnlyDictionary<Guid, Product> products, Guid productId, Guid? variantId)
    {
        if (!products.TryGetValue(productId, out var product))
            throw new NotFoundException($"Product {productId} not found.");
        if (!product.IsActive)
            throw new ConflictException($"Product '{product.Name}' is currently unavailable.");

        var activeVariants = product.Variants.Where(v => v.IsActive).ToList();

        if (variantId is { } vid)
        {
            var variant = activeVariants.FirstOrDefault(v => v.Id == vid)
                ?? throw new ConflictException($"The selected variant of '{product.Name}' is no longer available.");
            return ($"{product.Name} ({variant.Name})", variant.Price, product.Currency, product.Code, product.KitchenStationId);
        }

        if (activeVariants.Count > 0)
            throw new ConflictException($"'{product.Name}' requires a variant (e.g. {activeVariants[0].Name}).");

        return (product.Name, product.Price, product.Currency, product.Code, product.KitchenStationId);
    }
}
