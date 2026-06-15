using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Catalog;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = MediatR.Unit;

namespace BornoBit.Restaurant.Application.Products;

/// <summary>One DirectStock link: the inventory item a product/variant deducts. VariantId null = product-level.</summary>
public record VariantStockLinkInput(Guid? VariantId, Guid InventoryItemId);

/// <summary>
/// Sets how selling a product impacts stock. For <see cref="InventoryMethod.DirectStock"/>, links inventory
/// item(s) to this product — one per variant (each size its own SKU) or a single product-level item for a
/// non-variant product. Any DirectStock slot left without a provided link gets a SKU auto-created, so every
/// stock-tracked product/variant always has one. RecipeBased is configured via the recipe editor; this
/// command just sets the flag. Any method change first unlinks the product's existing DirectStock items.
/// </summary>
public record SetProductInventoryMethodCommand(
    Guid ProductId, InventoryMethod Method, IReadOnlyList<VariantStockLinkInput>? Links = null)
    : IRequest<Unit>;

public class SetProductInventoryMethodCommandValidator : AbstractValidator<SetProductInventoryMethodCommand>
{
    public SetProductInventoryMethodCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();

        // Links are optional — unlinked DirectStock slots are auto-filled — but any provided links must be sane.
        When(x => x.Method == InventoryMethod.DirectStock && x.Links is { Count: > 0 }, () =>
        {
            RuleFor(x => x.Links!)
                .Must(links => links.All(l => l.InventoryItemId != Guid.Empty))
                .WithMessage("Every stock link needs an item.")
                .Must(links => links.Select(l => l.InventoryItemId).Distinct().Count() == links.Count)
                .WithMessage("A stock item cannot back more than one variant.")
                .Must(links => links.Select(l => l.VariantId).Distinct().Count() == links.Count)
                .WithMessage("Each variant can be linked only once.");
        });
    }
}

public class SetProductInventoryMethodCommandHandler : IRequestHandler<SetProductInventoryMethodCommand, Unit>
{
    private readonly IAppDbContext _db;

    public SetProductInventoryMethodCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(SetProductInventoryMethodCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException($"Product {request.ProductId} not found.");

        // Unlink anything currently pointing at this product, so re-saving (or switching away from
        // DirectStock) never leaves a dangling item linked. Remember each slot's prior item so an unlinked
        // slot re-attaches its existing SKU instead of orphaning it + minting a duplicate.
        var linkedNow = await _db.InventoryItems.Where(i => i.ProductId == product.Id).ToListAsync(cancellationToken);
        var priorBySlot = new Dictionary<Guid, InventoryItem>(); // product-level slot keyed by Guid.Empty
        foreach (var item in linkedNow)
        {
            priorBySlot.TryAdd(item.VariantId ?? Guid.Empty, item);
            item.LinkToProduct(null, null);
        }

        if (request.Method == InventoryMethod.DirectStock)
        {
            var variantIds = product.Variants.Select(v => v.Id).ToHashSet();
            var links = request.Links ?? Array.Empty<VariantStockLinkInput>();

            foreach (var link in links)
            {
                if (link.VariantId is { } vid && !variantIds.Contains(vid))
                    throw new ConflictException("A stock link references a variant that is not on this product.");

                var item = await _db.InventoryItems.FirstOrDefaultAsync(i => i.Id == link.InventoryItemId, cancellationToken)
                    ?? throw new NotFoundException("Linked stock item not found.");

                // An item points at exactly one (product, variant). Reject if it still backs a different product.
                if (item.ProductId is { } pid && pid != product.Id)
                    throw new ConflictException($"Stock item '{item.Name}' is already linked to another product.");

                item.LinkToProduct(product.Id, link.VariantId);
            }

            // Fill every slot the caller left unlinked: re-attach the slot's prior SKU, else auto-create one,
            // so a stock-tracked product/variant always has a SKU (the sale-time "no SKU" warning then never
            // fires for a configured product).
            await FillMissingSkusAsync(product, links.Select(l => l.VariantId).ToHashSet(), priorBySlot, cancellationToken);
        }

        product.SetInventoryMethod(request.Method);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    /// <summary>
    /// Fills each variant slot (or the product-level slot when there are no variants) that has no provided
    /// link: re-attaches the slot's prior SKU if there was one, otherwise auto-creates a finished-good SKU
    /// (default category + PCS unit, generated unique code). No-op for a slot if no default category/unit
    /// exists (the sale-time warning then covers the gap).
    /// </summary>
    private async Task FillMissingSkusAsync(
        Domain.Catalog.Product product, HashSet<Guid?> linkedVariants,
        IReadOnlyDictionary<Guid, InventoryItem> priorBySlot, CancellationToken ct)
    {
        // Slots needing a SKU: one per variant, or a single product-level (null) slot when there are none.
        var slots = product.Variants.Count > 0
            ? product.Variants.Select(v => ((Guid?)v.Id, (string?)v.Name)).ToList()
            : new List<(Guid?, string?)> { (null, null) };

        var missing = slots.Where(s => !linkedVariants.Contains(s.Item1)).ToList();
        if (missing.Count == 0) return;

        InventoryCategory? category = null;
        Domain.Inventory.Unit? unit = null;
        HashSet<string>? existingCodes = null;

        foreach (var (variantId, variantName) in missing)
        {
            // Re-attach the slot's prior SKU (unlinked above, not claimed by a provided link) — don't duplicate.
            if (priorBySlot.TryGetValue(variantId ?? Guid.Empty, out var prior) && prior.ProductId is null)
            {
                prior.LinkToProduct(product.Id, variantId);
                continue;
            }

            category ??= await _db.InventoryCategories.FirstOrDefaultAsync(c => c.Name.Contains("Finished"), ct)
                ?? await _db.InventoryCategories.FirstOrDefaultAsync(c => c.IsActive, ct);
            unit ??= await _db.Units.FirstOrDefaultAsync(u => u.Code == "PCS", ct)
                ?? await _db.Units.FirstOrDefaultAsync(u => u.IsActive, ct);
            if (category is null || unit is null) return; // nothing sensible to default to

            existingCodes ??= (await _db.InventoryItems.Select(i => i.Code).ToListAsync(ct))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var code = UniqueCode(product.Code, variantName, existingCodes);
            var name = variantName is null ? product.Name : $"{product.Name} {variantName}";

            var item = InventoryItem.Create(
                code, name, category.Id, InventoryItemType.FinishedGood, unit.Id,
                productId: product.Id);
            item.LinkToProduct(product.Id, variantId);
            _db.InventoryItems.Add(item);
        }
    }

    private static string UniqueCode(string productCode, string? variantName, HashSet<string> taken)
    {
        static string Slug(string s) => new(s.Where(char.IsLetterOrDigit).ToArray());
        var baseCode = (Slug(productCode) + (variantName is null ? "" : "-" + Slug(variantName))).ToUpperInvariant();
        if (string.IsNullOrEmpty(baseCode)) baseCode = "SKU";

        var code = baseCode;
        for (var n = 2; taken.Contains(code); n++) code = $"{baseCode}-{n}";
        taken.Add(code);
        return code;
    }
}
