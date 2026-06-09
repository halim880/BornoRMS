namespace BornoBit.Restaurant.Web.Components.Hc.Dialog;

public sealed class HcDialogResult
{
    public bool Cancelled { get; init; }
    public object? Data { get; init; }

    public static HcDialogResult Ok(object? data = null) => new() { Cancelled = false, Data = data };
    public static HcDialogResult Cancel() => new() { Cancelled = true, Data = null };
}
