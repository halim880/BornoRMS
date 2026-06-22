namespace BornoBit.Restaurant.Web.Components.Pages.Admin.Kitchens;

public class KitchenFormModel
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? ColorHex { get; set; }
    public string? PrinterName { get; set; }
    public int DisplayOrder { get; set; }

    public Guid? SavedId { get; set; }
}
