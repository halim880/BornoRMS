using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Hc.Dialog;

public sealed class HcDialogService : IHcDialogService
{
    private readonly List<HcDialogInstance> _open = new();
    private readonly object _gate = new();

    public event Action? Changed;

    public IReadOnlyList<HcDialogInstance> Open
    {
        get { lock (_gate) return _open.ToList(); }
    }

    public async Task<HcDialogResult> ShowAsync<TComponent, TContent>(TContent content, HcDialogOptions? options = null)
        where TComponent : IComponent
    {
        var opts = options ?? new HcDialogOptions();
        var tcs = new TaskCompletionSource<HcDialogResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var parameters = new Dictionary<string, object?>
        {
            ["Content"] = content
        };
        var instance = new HcDialogInstance(Guid.NewGuid(), opts, typeof(TComponent), parameters, tcs);

        lock (_gate) _open.Add(instance);
        Changed?.Invoke();

        try
        {
            return await tcs.Task;
        }
        finally
        {
            lock (_gate) _open.Remove(instance);
            Changed?.Invoke();
        }
    }

    public async Task<bool> ConfirmAsync(string title, string message, string confirmLabel = "Confirm", string cancelLabel = "Cancel", string variant = "primary")
    {
        var content = new HcConfirmContent
        {
            Title = title,
            Message = message,
            ConfirmLabel = confirmLabel,
            CancelLabel = cancelLabel,
            Variant = variant
        };
        var result = await ShowAsync<HcConfirmDialog, HcConfirmContent>(content, new HcDialogOptions
        {
            Title = title,
            Width = "440px",
            DismissOnOverlayClick = true,
            DismissOnEsc = true
        });
        return !result.Cancelled && result.Data is true;
    }
}

public sealed class HcConfirmContent
{
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string ConfirmLabel { get; set; } = "Confirm";
    public string CancelLabel { get; set; } = "Cancel";
    public string Variant { get; set; } = "primary";
}
