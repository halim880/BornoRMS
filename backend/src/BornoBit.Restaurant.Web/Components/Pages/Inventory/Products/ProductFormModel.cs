using BornoBit.Restaurant.Domain.Catalog;

namespace BornoBit.Restaurant.Web.Components.Pages.Inventory.Products;

public class ProductFormModel
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? BanglaName { get; set; }
    public Guid ProductCategoryId { get; set; }
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public int DisplayOrder { get; set; }
    public Guid? KitchenStationId { get; set; }
    public List<VariantFormRow> Variants { get; set; } = new();

    /// <summary>How selling this product impacts stock. RecipeBased is configured in the recipe editor.</summary>
    public InventoryMethod InventoryMethod { get; set; } = InventoryMethod.None;
    /// <summary>For DirectStock on a product with NO variants: the stock item deducted per sale.</summary>
    public Guid? LinkedInventoryItemId { get; set; }

    public Guid? SavedId { get; set; }
}

public class VariantFormRow
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    /// <summary>For DirectStock on a product WITH variants: the stock item this variant deducts (its own SKU).</summary>
    public Guid? LinkedInventoryItemId { get; set; }
}
