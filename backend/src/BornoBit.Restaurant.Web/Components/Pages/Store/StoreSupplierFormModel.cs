namespace BornoBit.Restaurant.Web.Components.Pages.Store;

public class StoreSupplierFormModel
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public int PaymentTermsDays { get; set; }
    public string? Notes { get; set; }

    public Guid? SavedId { get; set; }
}
