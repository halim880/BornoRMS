namespace BornoBit.Restaurant.Web.Components.Hc.Toast;

public sealed class HcToastService : IHcToastService
{
    private readonly List<HcToastEntry> _entries = new();
    private readonly object _gate = new();

    public event Action? Changed;

    public IReadOnlyList<HcToastEntry> Entries
    {
        get { lock (_gate) return _entries.ToList(); }
    }

    private static int DefaultDurationFor(HcToastKind kind) => kind switch
    {
        HcToastKind.Error => 6000,
        HcToastKind.Warning => 5000,
        _ => 3500
    };

    public void Show(HcToastKind kind, string message, string? title = null, int? durationMs = null)
    {
        var entry = new HcToastEntry
        {
            Kind = kind,
            Message = message,
            Title = title,
            DurationMs = durationMs ?? DefaultDurationFor(kind)
        };
        lock (_gate) _entries.Add(entry);
        Changed?.Invoke();
    }

    public void ShowSuccess(string message, string? title = null, int? durationMs = null) => Show(HcToastKind.Success, message, title, durationMs);
    public void ShowError(string message, string? title = null, int? durationMs = null) => Show(HcToastKind.Error, message, title, durationMs);
    public void ShowWarning(string message, string? title = null, int? durationMs = null) => Show(HcToastKind.Warning, message, title, durationMs);
    public void ShowInfo(string message, string? title = null, int? durationMs = null) => Show(HcToastKind.Info, message, title, durationMs);

    public void Dismiss(Guid id)
    {
        lock (_gate)
        {
            var idx = _entries.FindIndex(e => e.Id == id);
            if (idx < 0) return;
            _entries.RemoveAt(idx);
        }
        Changed?.Invoke();
    }

    public void Clear()
    {
        lock (_gate) _entries.Clear();
        Changed?.Invoke();
    }
}
