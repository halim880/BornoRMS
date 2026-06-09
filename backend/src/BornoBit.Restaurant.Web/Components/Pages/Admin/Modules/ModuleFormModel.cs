namespace BornoBit.Restaurant.Web.Components.Pages.Admin.Modules;

public class ModuleFormModel
{
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public string? RequiredRole { get; set; }

    public Guid? SavedId { get; set; }
}
