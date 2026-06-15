namespace BornoBit.Restaurant.Reporting.Models;

public record StockValuationReportData(
    string RestaurantName,
    DateTime GeneratedAtUtc,
    string Currency,
    decimal GrandTotal,
    IReadOnlyList<StockValuationLine> Lines,
    string? FilterNote = null
);

public record StockValuationLine(
    string Category,
    string Code,
    string Name,
    string ItemType,
    decimal QtyOnHand,
    string UnitCode,
    decimal ReorderLevel,
    decimal AvgCost,
    decimal StockValue,
    bool IsLowStock
);
