using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Hc.Dialog;

/// <summary>
/// Per-dialog handle cascaded into the content component. Use Close/Cancel from the content
/// to dismiss the dialog and return a result.
/// </summary>
public sealed class HcDialogInstance
{
    public Guid Id { get; }
    public HcDialogOptions Options { get; }
    public Type ContentComponent { get; }
    public IDictionary<string, object?> ContentParameters { get; }
    private readonly TaskCompletionSource<HcDialogResult> _tcs;

    public Task<HcDialogResult> Result => _tcs.Task;

    internal HcDialogInstance(
        Guid id,
        HcDialogOptions options,
        Type contentComponent,
        IDictionary<string, object?> contentParameters,
        TaskCompletionSource<HcDialogResult> tcs)
    {
        Id = id;
        Options = options;
        ContentComponent = contentComponent;
        ContentParameters = contentParameters;
        _tcs = tcs;
    }

    public void Close(object? data = null)
    {
        _tcs.TrySetResult(HcDialogResult.Ok(data));
    }

    public void Cancel()
    {
        _tcs.TrySetResult(HcDialogResult.Cancel());
    }
}
