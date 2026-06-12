namespace BornoBit.Restaurant.Web.Services;

public sealed class CustomerSiteOptions
{
    public const string SectionName = "CustomerSite";

    /// <summary>Public base URL of the customer ordering site, encoded into table QR codes.</summary>
    public string BaseUrl { get; set; } = "http://localhost:3000";
}
