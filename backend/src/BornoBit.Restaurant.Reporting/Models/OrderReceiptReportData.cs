namespace BornoBit.Restaurant.Reporting.Models;

public record OrderReceiptReportData(
    string RestaurantName,
    string OrderNumber,
    DateTime OrderedAtUtc,
    string OrderType,
    string Status,
    string? TableNumber,
    string? CustomerName,
    string CustomerPhone,
    string Currency,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal Total,
    bool IsPaid,
    string? PaymentMethod,
    decimal? AmountTendered,
    decimal? ChangeGiven,
    string? Notes,
    DateTime GeneratedAtUtc,
    IReadOnlyList<OrderReceiptLine> Lines,
    decimal RoundingAdjustment = 0m,
    decimal VatAmount = 0m,
    decimal ServiceChargeAmount = 0m,
    string? CashierName = null,
    ReceiptBranding? Branding = null
);

public record OrderReceiptLine(
    string Code,
    string Name,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);
