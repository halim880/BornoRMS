namespace BornoBit.Restaurant.Web.Components.Hc;

/// <summary>
/// One entry in an <see cref="HcBreadcrumb"/> trail. Set <see cref="Href"/>
/// for clickable trail steps; leave it null for the current page.
/// </summary>
public sealed record HcBreadcrumbItem(string Label, string? Href = null, string? Icon = null);
