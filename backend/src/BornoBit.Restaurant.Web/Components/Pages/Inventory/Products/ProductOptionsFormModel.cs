namespace BornoBit.Restaurant.Web.Components.Pages.Inventory.Products;

/// <summary>Edit model for a product's modifier / add-on option groups (separate dialog from the product form).</summary>
public class ProductOptionsFormModel
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public List<OptionGroupFormRow> Groups { get; set; } = new();
    public bool Saved { get; set; }
}

public class OptionGroupFormRow
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BanglaName { get; set; }
    /// <summary>Single-select (radio) when true; multi-select (checkboxes) when false.</summary>
    public bool IsSingle { get; set; } = true;
    /// <summary>At least one option must be chosen.</summary>
    public bool IsRequired { get; set; }
    public List<OptionFormRow> Options { get; set; } = new();
}

public class OptionFormRow
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BanglaName { get; set; }
    public decimal PriceDelta { get; set; }
    /// <summary>Optional inventory item this add-on consumes when chosen (null = no stock impact).</summary>
    public Guid? InventoryItemId { get; set; }
    /// <summary>How much of the item to deduct per line unit, in the item's base unit.</summary>
    public decimal ConsumeQtyBase { get; set; }
}
