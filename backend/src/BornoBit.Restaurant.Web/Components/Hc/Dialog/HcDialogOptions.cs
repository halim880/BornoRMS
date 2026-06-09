namespace BornoBit.Restaurant.Web.Components.Hc.Dialog;

public sealed class HcDialogOptions
{
    public string? Title { get; set; }
    /// <summary>CSS width — e.g. "480px", "min(900px, 90vw)". Default: 520px.</summary>
    public string Width { get; set; } = "520px";
    /// <summary>Prevent closing when overlay is clicked.</summary>
    public bool DismissOnOverlayClick { get; set; } = true;
    /// <summary>Prevent closing on ESC.</summary>
    public bool DismissOnEsc { get; set; } = true;
    public bool ShowCloseButton { get; set; } = true;
}
