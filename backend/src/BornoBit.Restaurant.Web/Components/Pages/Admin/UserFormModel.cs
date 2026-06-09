namespace BornoBit.Restaurant.Web.Components.Pages.Admin;

public class UserFormModel
{
    public Guid? Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public List<string> Roles { get; set; } = new();
    public string? InitialPassword { get; set; }
}
