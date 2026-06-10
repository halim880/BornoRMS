using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.BornoUi.Dialog;

/// <summary>
/// Per-dialog handle cascaded into the content component. Use Close/Cancel from the content
/// to dismiss the dialog and return a result.
/// </summary>
public sealed class BoDialogInstance
{
    public Guid Id { get; }
    public BoDialogOptions Options { get; }
    public Type ContentComponent { get; }
    public IDictionary<string, object?> ContentParameters { get; }
    private readonly TaskCompletionSource<BoDialogResult> _tcs;

    public Task<BoDialogResult> Result => _tcs.Task;

    internal BoDialogInstance(
        Guid id,
        BoDialogOptions options,
        Type contentComponent,
        IDictionary<string, object?> contentParameters,
        TaskCompletionSource<BoDialogResult> tcs)
    {
        Id = id;
        Options = options;
        ContentComponent = contentComponent;
        ContentParameters = contentParameters;
        _tcs = tcs;
    }

    public void Close(object? data = null)
    {
        _tcs.TrySetResult(BoDialogResult.Ok(data));
    }

    public void Cancel()
    {
        _tcs.TrySetResult(BoDialogResult.Cancel());
    }
}
