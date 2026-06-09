using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Hc.Dialog;

public interface IHcDialogService
{
    /// <summary>
    /// Show a typed dialog. TComponent is the content component (must implement
    /// IHcDialogContent&lt;TContent&gt; via a [Parameter] called Content). Returns when the dialog closes.
    /// </summary>
    Task<HcDialogResult> ShowAsync<TComponent, TContent>(TContent content, HcDialogOptions? options = null)
        where TComponent : IComponent;

    /// <summary>Convenience confirm dialog returning bool (true = confirmed).</summary>
    Task<bool> ConfirmAsync(string title, string message, string confirmLabel = "Confirm", string cancelLabel = "Cancel", string variant = "primary");
}
