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
    public static (string Name, decimal Price, string Currency, string Code, Guid? StationId, int PrepMinutes) Resolve(
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
            return ($"{product.Name} ({variant.Name})", variant.Price, product.Currency, product.Code, product.KitchenStationId, product.PrepMinutes);
        }

        if (activeVariants.Count > 0)
            throw new ConflictException($"'{product.Name}' requires a variant (e.g. {activeVariants[0].Name}).");

        return (product.Name, product.Price, product.Currency, product.Code, product.KitchenStationId, product.PrepMinutes);
    }

    /// <summary>
    /// Validates a set of chosen option ids against the product's modifier / add-on groups and returns the
    /// frozen snapshot rows to stamp onto the order line. Enforces each group's min/max selection rules and
    /// that every option is active and belongs to the product. Requires the product's OptionGroups + Options loaded.
    /// </summary>
    public static IReadOnlyList<(Guid OptionId, string GroupName, string OptionName, decimal PriceDelta)> ResolveModifiers(
        IReadOnlyDictionary<Guid, Product> products, Guid productId, IReadOnlyList<Guid>? optionIds)
    {
        if (!products.TryGetValue(productId, out var product))
            throw new NotFoundException($"Product {productId} not found.");

        var selected = optionIds?.Distinct().ToList() ?? new List<Guid>();
        var rows = new List<(Guid, string, string, decimal)>();

        foreach (var oid in selected)
        {
            var group = product.OptionGroups.FirstOrDefault(g => g.IsActive && g.Options.Any(o => o.Id == oid))
                ?? throw new ConflictException("A selected option is no longer available.");
            var opt = group.Options.First(o => o.Id == oid);
            if (!opt.IsActive) throw new ConflictException($"The option '{opt.Name}' is no longer available.");
            rows.Add((opt.Id, group.Name, opt.Name, opt.PriceDelta));
        }

        // Enforce each group's selection rules against what was chosen.
        foreach (var group in product.OptionGroups.Where(g => g.IsActive))
        {
            var count = selected.Count(id => group.Options.Any(o => o.Id == id));
            if (count < group.MinSelections)
                throw new ConflictException($"'{group.Name}' requires at least {group.MinSelections} selection(s) on '{product.Name}'.");
            if (count > group.MaxSelections)
                throw new ConflictException($"'{group.Name}' allows at most {group.MaxSelections} selection(s) on '{product.Name}'.");
        }

        return rows;
    }
}
