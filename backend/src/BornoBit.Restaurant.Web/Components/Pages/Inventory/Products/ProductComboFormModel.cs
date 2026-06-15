namespace BornoBit.Restaurant.Web.Components.Pages.Inventory.Products;

/// <summary>Edit model for a product's combo flag + component list (separate dialog from the product form).</summary>
public class ProductComboFormModel
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public bool IsCombo { get; set; }
    public List<ComboComponentFormRow> Components { get; set; } = new();
    public bool Saved { get; set; }
}

public class ComboComponentFormRow
{
    public Guid? Id { get; set; }
    public Guid ComponentProductId { get; set; }
    public int Quantity { get; set; } = 1;
}
