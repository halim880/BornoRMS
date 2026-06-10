namespace BornoBit.Restaurant.Web.Components.BornoUi.Toast;

public sealed class BoToastService : IBoToastService
{
    private readonly List<BoToastEntry> _entries = new();
    private readonly object _gate = new();

    public event Action? Changed;

    public IReadOnlyList<BoToastEntry> Entries
    {
        get { lock (_gate) return _entries.ToList(); }
    }

    private static int DefaultDurationFor(BoToastKind kind) => kind switch
    {
        BoToastKind.Error => 6000,
        BoToastKind.Warning => 5000,
        _ => 3500
    };

    public void Show(BoToastKind kind, string message, string? title = null, int? durationMs = null)
    {
        var entry = new BoToastEntry
        {
            Kind = kind,
            Message = message,
            Title = title,
            DurationMs = durationMs ?? DefaultDurationFor(kind)
        };
        lock (_gate) _entries.Add(entry);
        Changed?.Invoke();
    }

    public void ShowSuccess(string message, string? title = null, int? durationMs = null) => Show(BoToastKind.Success, message, title, durationMs);
    public void ShowError(string message, string? title = null, int? durationMs = null) => Show(BoToastKind.Error, message, title, durationMs);
    public void ShowWarning(string message, string? title = null, int? durationMs = null) => Show(BoToastKind.Warning, message, title, durationMs);
    public void ShowInfo(string message, string? title = null, int? durationMs = null) => Show(BoToastKind.Info, message, title, durationMs);

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
