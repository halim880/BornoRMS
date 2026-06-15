namespace BornoBit.Restaurant.Web.Components.Pages.Stock;

/// <summary>Create-a-SKU-for-a-product/variant form state (Product SKUs page). Always a finished good.</summary>
public class SkuFormModel
{
    // Context (set by the page; shown read-only in the dialog header).
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? VariantName { get; set; }

    // SKU fields.
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? BanglaName { get; set; }
    public Guid InventoryCategoryId { get; set; }
    public Guid BaseUnitId { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal ReorderQty { get; set; }
    public bool IsPerishable { get; set; }
    public decimal? PackSize { get; set; }
    public string? PackNote { get; set; }

    public Guid? SavedId { get; set; }
}
