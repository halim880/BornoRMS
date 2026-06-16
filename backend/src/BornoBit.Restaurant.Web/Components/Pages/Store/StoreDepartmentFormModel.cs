namespace BornoBit.Restaurant.Web.Components.Pages.Store;

public class StoreDepartmentFormModel
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? BanglaName { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }

    public Guid? SavedId { get; set; }
}
