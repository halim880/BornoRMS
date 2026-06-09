namespace BornoBit.Restaurant.Web.Components.Hc.Toast;

public sealed class HcToastEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public HcToastKind Kind { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Title { get; init; }
    public int DurationMs { get; init; } = 4000;
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
