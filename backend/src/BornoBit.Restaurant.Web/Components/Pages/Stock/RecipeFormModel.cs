namespace BornoBit.Restaurant.Web.Components.Pages.Stock;

/// <summary>Edit model passed to <c>RecipeFormDialog</c>.</summary>
public class RecipeFormModel
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = "";
    /// <summary>True when editing an existing recipe (product is then fixed).</summary>
    public bool IsExisting { get; set; }
    public decimal Yield { get; set; } = 1m;
    public List<RecipeRow> Items { get; set; } = new();

    public bool Saved { get; set; }

    public class RecipeRow
    {
        public Guid? Id { get; set; }
        public Guid InventoryItemId { get; set; }
        public decimal Quantity { get; set; } = 1m;
        public Guid UnitId { get; set; }
    }
}
