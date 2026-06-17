namespace BornoBit.Restaurant.Reporting.Models;

public record SalesInvoiceReportData(
    string RestaurantName,
    DateTime FromUtc,
    DateTime ToUtc,
    DateTime GeneratedAtUtc,
    string Currency,
    int TotalInvoices,
    decimal TotalSubtotal,
    decimal TotalDiscount,
    decimal GrandTotal,
    IReadOnlyList<SalesInvoiceReportRow> Rows
);

public record SalesInvoiceReportRow(
    DateTime PaidAtUtc,
    string OrderNumber,
    string CustomerName,
    string OrderType,
    string PaymentMethod,
    decimal Subtotal,
    decimal Discount,
    decimal Total
);
