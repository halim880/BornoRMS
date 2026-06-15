namespace BornoBit.Restaurant.Reporting.Models;

public record GoodsReceiptReportData(
    string RestaurantName,
    string GrnNumber,
    string SupplierName,
    string? InvoiceNo,
    DateTime ReceivedAtUtc,
    string Status,
    string Currency,
    string? Notes,
    decimal Subtotal,
    DateTime GeneratedAtUtc,
    IReadOnlyList<GoodsReceiptReportLine> Lines
);

public record GoodsReceiptReportLine(
    string ItemName,
    decimal Qty,
    string UnitCode,
    decimal UnitCost,
    decimal LineTotal
);
