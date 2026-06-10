namespace BornoBit.Restaurant.Web.Components.BornoUi;

/// <summary>
/// One entry in an <see cref="BoBreadcrumb"/> trail. Set <see cref="Href"/>
/// for clickable trail steps; leave it null for the current page.
/// </summary>
public sealed record BoBreadcrumbItem(string Label, string? Href = null, string? Icon = null);
