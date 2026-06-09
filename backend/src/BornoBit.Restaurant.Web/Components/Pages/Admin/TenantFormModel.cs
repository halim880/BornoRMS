namespace BornoBit.Restaurant.Web.Components.Pages.Admin;

public class TenantFormModel
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
    public string? Subdomain { get; set; }
    public string? ContactEmail { get; set; }
    public DateTime? LicenseExpiresOnUtc { get; set; }
}
