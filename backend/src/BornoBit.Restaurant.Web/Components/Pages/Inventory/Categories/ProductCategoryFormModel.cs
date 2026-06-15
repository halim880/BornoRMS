namespace BornoBit.Restaurant.Web.Components.Pages.Inventory.Categories;

public class ProductCategoryFormModel
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }

    /// <summary>Per-category VAT rate (%). Null inherits the restaurant default VAT.</summary>
    public decimal? TaxRatePercent { get; set; }

    public Guid? SavedId { get; set; }
}
