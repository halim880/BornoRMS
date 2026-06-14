namespace BornoBit.Restaurant.Application.Operations.Dashboard;

/// <summary>
/// Table status is not stored — it is derived from open orders + today's reservations:
/// Available (no open order/booking), Occupied (open unpaid order), WaitingPayment (served, unpaid),
/// Reserved (a Booked reservation for today).
/// </summary>
public enum DerivedTableStatus
{
    Available = 0,
    Occupied = 1,
    Reserved = 2,
    WaitingPayment = 3
}

/// <summary>Date-range presets shared by the analytics widgets; the actual window is computed page-side.</summary>
public enum DashboardRange
{
    Today = 0,
    Yesterday = 1,
    Last7Days = 2,
    ThisMonth = 3,
    Custom = 99
}
