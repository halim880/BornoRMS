namespace BornoBit.Restaurant.Web.Components.BornoUi.Dialog;

public sealed class BoDialogResult
{
    public bool Cancelled { get; init; }
    public object? Data { get; init; }

    public static BoDialogResult Ok(object? data = null) => new() { Cancelled = false, Data = data };
    public static BoDialogResult Cancel() => new() { Cancelled = true, Data = null };
}
