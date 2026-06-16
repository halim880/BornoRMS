namespace BornoBit.Restaurant.Web.Components.Pages.Store;

public class StoreOpeningModel
{
    public Guid ItemId { get; set; }
    public Guid BaseUnitId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public string Currency { get; set; } = "Tk";
    public decimal Qty { get; set; }
    public decimal UnitCost { get; set; }
    public bool Saved { get; set; }
}
