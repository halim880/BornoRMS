using BornoBit.Restaurant.Application.Operations.Dashboard;

namespace BornoBit.Restaurant.Web.Components.Shared;

/// <summary>
/// One place mapping a <see cref="DerivedTableStatus"/> to its badge tone and friendly label, so the
/// POS, Waiter floor and Dashboard live floor all show table status identically.
/// </summary>
public static class TableStatusDisplay
{
    public static string Tone(DerivedTableStatus s) => s switch
    {
        DerivedTableStatus.Available => "success",
        DerivedTableStatus.Occupied => "warning",
        DerivedTableStatus.Reserved => "info",
        DerivedTableStatus.WaitingPayment => "danger",
        _ => "neutral"
    };

    public static string Label(DerivedTableStatus s) => s switch
    {
        DerivedTableStatus.Available => "Available",
        DerivedTableStatus.Occupied => "Occupied",
        DerivedTableStatus.Reserved => "Reserved",
        DerivedTableStatus.WaitingPayment => "Waiting payment",
        _ => s.ToString()
    };
}
