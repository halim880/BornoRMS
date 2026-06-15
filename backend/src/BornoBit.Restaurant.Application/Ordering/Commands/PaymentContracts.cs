using BornoBit.Restaurant.Domain.Ordering;

namespace BornoBit.Restaurant.Application.Ordering.Commands;

/// <summary>One tender in a settlement — many of these make a split payment.</summary>
public record PaymentEntryInput(
    PaymentMethod Method,
    PaymentProvider? Provider,
    decimal Amount,
    decimal Tendered,
    string? Reference = null);

/// <summary>A single payment row as shown in history / the ledger.</summary>
public record PaymentLineDto(
    Guid PaymentId,
    PaymentMethod Method,
    PaymentProvider? Provider,
    decimal Amount,
    decimal Tendered,
    decimal Change,
    PaymentKind Kind,
    PaymentEntryStatus Status,
    DateTime CreatedAtUtc,
    string? CashierName,
    string? Reference);

/// <summary>Outcome of any operation that mutates an order's payments (add/void/refund/settle).</summary>
public record SettlementResultDto(
    Guid OrderId,
    decimal GrandTotal,
    decimal AmountPaid,
    decimal BalanceDue,
    PaymentStatus PaymentStatus,
    decimal Change,
    IReadOnlyList<PaymentLineDto> Payments,
    IReadOnlyList<string> Warnings);

public static class PaymentMapping
{
    public static PaymentLineDto ToDto(this Payment p) => new(
        p.Id, p.Method, p.Provider, p.Amount, p.Tendered, p.Change, p.Kind, p.Status, p.CreatedAtUtc, p.CashierName, p.Reference);

    public static SettlementResultDto ToSettlementResult(
        this Order order, decimal change = 0m, IReadOnlyList<string>? warnings = null) => new(
        order.Id,
        order.GrandTotal,
        order.AmountPaid,
        order.BalanceDue,
        order.PaymentStatus,
        change,
        order.Payments.OrderBy(p => p.CreatedAtUtc).Select(p => p.ToDto()).ToList(),
        warnings ?? Array.Empty<string>());
}
