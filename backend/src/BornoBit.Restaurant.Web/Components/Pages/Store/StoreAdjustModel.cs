namespace BornoBit.Restaurant.Web.Components.Pages.Store;

public class StoreAdjustModel
{
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public decimal CurrentQty { get; set; }
    public decimal CountedQty { get; set; }
    public string? Reason { get; set; }
    public bool Saved { get; set; }
}
