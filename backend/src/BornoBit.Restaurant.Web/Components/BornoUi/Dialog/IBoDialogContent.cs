namespace BornoBit.Restaurant.Web.Components.BornoUi.Dialog;

/// <summary>
/// Marker interface for components that can be hosted inside an BoDialog. The component
/// receives the content payload via a [Parameter] named "Content" and a cascaded BoDialogInstance.
/// </summary>
public interface IBoDialogContent<TContent>
{
    TContent Content { get; set; }
}
