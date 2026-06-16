namespace BornoBit.Restaurant.Reporting.Models;

public record StoreDepartmentConsumptionReportData(
    string RestaurantName,
    DateTime FromUtc,
    DateTime ToUtc,
    string Currency,
    decimal GrandTotalValue,
    DateTime GeneratedAtUtc,
    IReadOnlyList<StoreDepartmentConsumptionReportRow> Rows
);

public record StoreDepartmentConsumptionReportRow(
    string DepartmentName,
    decimal TotalValue,
    IReadOnlyList<StoreDepartmentConsumptionReportItem> Items
);

public record StoreDepartmentConsumptionReportItem(
    string ItemName,
    string BaseUnitCode,
    decimal QtyBase,
    decimal Value
);
