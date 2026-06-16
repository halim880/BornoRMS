namespace BornoBit.Restaurant.Reporting.Models;

public record StoreMovementLedgerReportData(
    string RestaurantName,
    string? ItemName,
    DateTime? FromUtc,
    DateTime? ToUtc,
    decimal? OpeningBalance,
    decimal? ClosingBalance,
    string? UnitCode,
    DateTime GeneratedAtUtc,
    IReadOnlyList<StoreMovementLedgerLine> Lines
);

public record StoreMovementLedgerLine(
    DateTime OccurredAtUtc,
    string ItemName,
    string UnitCode,
    string MovementType,
    decimal QtyBase,
    string? Reason,
    decimal? RunningBalance
);
