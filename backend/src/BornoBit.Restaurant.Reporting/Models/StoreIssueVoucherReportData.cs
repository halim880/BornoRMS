namespace BornoBit.Restaurant.Reporting.Models;

public record StoreIssueVoucherReportData(
    string RestaurantName,
    string IssueNumber,
    string Destination,
    DateTime IssuedAtUtc,
    string Status,
    string? Notes,
    DateTime GeneratedAtUtc,
    IReadOnlyList<StoreIssueVoucherLine> Lines,
    string? RequisitionNumber = null
);

public record StoreIssueVoucherLine(
    string ItemName,
    decimal Qty,
    string UnitCode
);
