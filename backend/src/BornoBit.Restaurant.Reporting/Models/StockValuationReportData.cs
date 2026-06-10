namespace BornoBit.Restaurant.Reporting.Models;

public record StockValuationReportData(
    string RestaurantName,
    DateTime GeneratedAtUtc,
    string Currency,
    decimal GrandTotal,
    IReadOnlyList<StockValuationLine> Lines
);

public record StockValuationLine(
    string Category,
    string Code,
    string Name,
    decimal QtyOnHand,
    string UnitCode,
    decimal AvgCost,
    decimal StockValue,
    bool IsLowStock
);
