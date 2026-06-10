namespace BornoBit.Restaurant.Web.Components.BornoUi.Toast;

public sealed class BoToastEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public BoToastKind Kind { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Title { get; init; }
    public int DurationMs { get; init; } = 4000;
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
