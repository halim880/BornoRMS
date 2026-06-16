namespace BornoBit.PrintAgent;

// Wire contract — kept byte-compatible with the server's
// BornoBit.Restaurant.Web.Services.Printing.PrintJobContracts. Tolerant JSON + SchemaVersion
// keep the two deployables decoupled (the server and agent are released independently).

public sealed record PrintJobRequest
{
    public int SchemaVersion { get; init; } = 1;
    public Guid JobId { get; init; }
    public Guid OrderId { get; init; }
    public bool IsReprint { get; init; }
    public int Copies { get; init; } = 1;
    public string? PrinterName { get; init; }
    public bool OpenCashDrawer { get; init; }
    public ReceiptPayload? Receipt { get; init; }
    public KitchenTicketPayload? KitchenTicket { get; init; }
}

public sealed record KitchenTicketPayload
{
    public string RestaurantName { get; init; } = "";
    public string? TimeZoneId { get; init; }

    public string OrderNumber { get; init; } = "";
    public string? TicketLabel { get; init; }
    public string? OrderType { get; init; }
    public string? TableNumber { get; init; }
    public string? CustomerName { get; init; }
    public DateTime OrderedAtUtc { get; init; }

    public bool IsPriority { get; init; }
    public string? KitchenNotes { get; init; }
    public string? Notes { get; init; }

    public IReadOnlyList<KitchenTicketLinePayload> Lines { get; init; } = [];
}

public sealed record KitchenTicketLinePayload
{
    public string Name { get; init; } = "";
    public int Quantity { get; init; }
    public string? Notes { get; init; }
    public string? StationName { get; init; }
    public IReadOnlyList<string> Modifiers { get; init; } = [];
}

public sealed record ReceiptPayload
{
    public string RestaurantName { get; init; } = "";
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? VatRegistrationNo { get; init; }
    public string? Website { get; init; }
    public string? ThankYouLine { get; init; }
    public string? VisitAgainLine { get; init; }
    public string? TimeZoneId { get; init; }

    public string OrderNumber { get; init; } = "";
    public string? OrderType { get; init; }
    public string? TableNumber { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string? CashierName { get; init; }
    public string? Notes { get; init; }

    public DateTime OrderedAtUtc { get; init; }
    public DateTime? PaidAtUtc { get; init; }

    public string Currency { get; init; } = "";
    public IReadOnlyList<ReceiptLine> Lines { get; init; } = [];

    public decimal Subtotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal RoundingAdjustment { get; init; }
    public decimal VatAmount { get; init; }
    public decimal ServiceChargeAmount { get; init; }
    public decimal Total { get; init; }

    public bool IsPaid { get; init; }
    public string? PaymentMethod { get; init; }
    public decimal? AmountTendered { get; init; }
    public decimal? ChangeGiven { get; init; }
}

public sealed record ReceiptLine
{
    public string? Code { get; init; }
    public string Name { get; init; } = "";
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}

public sealed record PrintJobResponse(Guid JobId, string Status, bool Deduplicated, string? Message = null);
