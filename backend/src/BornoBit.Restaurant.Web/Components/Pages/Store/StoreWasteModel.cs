namespace BornoBit.Restaurant.Web.Components.Pages.Store;

public class StoreWasteModel
{
    public Guid ItemId { get; set; }
    public Guid BaseUnitId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public decimal CurrentQty { get; set; }
    public decimal Qty { get; set; }
    public string? Reason { get; set; }
    public bool Saved { get; set; }
}
