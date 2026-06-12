namespace BornoBit.Restaurant.Web.Components.Pages.Inventory.Tables;

public class TableFormModel
{
    public Guid? Id { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public int Capacity { get; set; } = 4;

    public Guid? SavedId { get; set; }
}
