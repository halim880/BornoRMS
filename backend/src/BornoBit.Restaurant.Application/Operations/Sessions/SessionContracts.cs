using BornoBit.Restaurant.Domain.Dining;

namespace BornoBit.Restaurant.Application.Operations.Sessions;

/// <summary>A dining session as shown on the floor / "my sessions" list.</summary>
public record SessionRowDto(
    Guid Id,
    string SessionNumber,
    Guid RestaurantTableId,
    string TableNumber,
    int GuestCount,
    Guid? WaiterUserId,
    string? WaiterName,
    DiningSessionStatus Status,
    DateTime OpenedAtUtc,
    int SessionMinutes,
    int OrderCount,
    decimal RunningBill,
    string Currency);

/// <summary>Running bill for a session: every order's lines plus the aggregated charges.</summary>
public record SessionBillDto(
    Guid SessionId,
    string SessionNumber,
    string TableNumber,
    int GuestCount,
    IReadOnlyList<SessionBillOrderDto> Orders,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal ServiceChargeAmount,
    decimal RoundingAdjustment,
    decimal GrandTotal,
    decimal PaidAmount,
    decimal BalanceDue,
    string Currency);

public record SessionBillOrderDto(
    Guid OrderId,
    string OrderNumber,
    string Status,
    bool IsPaid,
    decimal OrderTotal,
    IReadOnlyList<SessionBillLineDto> Lines);

public record SessionBillLineDto(string Name, int Quantity, decimal UnitPrice, decimal LineTotal);

/// <summary>Waiter dashboard widget counters (the top strip of the console).</summary>
public record WaiterDashboardDto(
    int MyTables,
    int AvailableTables,
    int OccupiedTables,
    int PendingRequests,
    int ReadyToServeOrders,
    int BillsWaiting,
    int MyActiveSessions,
    decimal MyRevenueServedToday,
    string Currency);

/// <summary>An order that is cooked and ready to carry to the table.</summary>
public record ReadyToServeRowDto(
    Guid OrderId,
    string OrderNumber,
    Guid? RestaurantTableId,
    string? TableNumber,
    Guid? DiningSessionId,
    DateTime? ReadyAtUtc,
    int WaitingMinutes,
    IReadOnlyList<ReadyToServeLineDto> Items);

public record ReadyToServeLineDto(string Name, int Quantity, string? StationName);
