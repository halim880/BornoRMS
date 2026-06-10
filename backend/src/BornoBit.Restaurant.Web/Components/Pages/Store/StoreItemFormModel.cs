namespace BornoBit.Restaurant.Web.Components.Pages.Store;

public class StoreItemFormModel
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? BanglaName { get; set; }
    public Guid StoreCategoryId { get; set; }
    public Guid BaseUnitId { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal ReorderQty { get; set; }
    public bool IsPerishable { get; set; }
    public decimal? PackSize { get; set; }
    public string? PackNote { get; set; }

    public Guid? SavedId { get; set; }
}
