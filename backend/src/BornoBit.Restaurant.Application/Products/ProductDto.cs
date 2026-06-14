namespace BornoBit.Restaurant.Application.Products;

public record ProductVariantDto(
    Guid Id,
    string Name,
    decimal Price,
    int DisplayOrder);

public record ProductDto(
    Guid Id,
    string Code,
    string Name,
    string? BanglaName,
    Guid ProductCategoryId,
    string CategoryName,
    decimal Price,
    string Currency,
    string? Description,
    string? ImagePath,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyList<ProductVariantDto> Variants,
    Guid? KitchenStationId = null,
    BornoBit.Restaurant.Domain.Catalog.InventoryMethod InventoryMethod = BornoBit.Restaurant.Domain.Catalog.InventoryMethod.None)
{
    public bool HasVariants => Variants.Count > 0;

    /// <summary>Lowest sellable price — the cheapest variant, or the base price when no variants.</summary>
    public decimal MinPrice => HasVariants ? Variants.Min(v => v.Price) : Price;
}
