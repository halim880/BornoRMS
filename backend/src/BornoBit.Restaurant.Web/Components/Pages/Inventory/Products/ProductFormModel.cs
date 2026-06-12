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
    public List<VariantFormRow> Variants { get; set; } = new();

    public Guid? SavedId { get; set; }
}

public class VariantFormRow
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
