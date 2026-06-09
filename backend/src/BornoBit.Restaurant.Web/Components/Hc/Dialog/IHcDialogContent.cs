namespace BornoBit.Restaurant.Web.Components.Hc.Dialog;

/// <summary>
/// Marker interface for components that can be hosted inside an HcDialog. The component
/// receives the content payload via a [Parameter] named "Content" and a cascaded HcDialogInstance.
/// </summary>
public interface IHcDialogContent<TContent>
{
    TContent Content { get; set; }
}
