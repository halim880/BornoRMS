using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Reports;

/// <summary>
/// Reconciles a business day's operational takings (POS payment ledger) against what actually reached the
/// books via the Cash Counter import (Income FinanceTransactions). Surfaces the two big ways they diverge:
/// orders paid but never imported, and payment methods with no matching cash account (so the import skips
/// them). When both are clear and the totals match, the day is reconciled.
/// </summary>
public record GetSalesGlReconciliationQuery(DateOnly Date) : IRequest<SalesGlReconciliationDto>;

public record SalesGlReconciliationDto(
    DateOnly Date,
    string Currency,
    decimal OperationalTakings,
    decimal PostedToBooks,
    decimal Variance,
    int UnaccountedOrders,
    decimal UnaccountedAmount,
    IReadOnlyList<string> BlockedMethods,
    bool IsReconciled);

public class GetSalesGlReconciliationQueryHandler : IRequestHandler<GetSalesGlReconciliationQuery, SalesGlReconciliationDto>
{
    private readonly IAppDbContext _db;
    private readonly IBusinessClock _clock;

    public GetSalesGlReconciliationQueryHandler(IAppDbContext db, IBusinessClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<SalesGlReconciliationDto> Handle(GetSalesGlReconciliationQuery request, CancellationToken cancellationToken)
    {
        var (start, end) = _clock.DayWindowUtc(request.Date);

        var paid = await _db.Orders
            .Where(o => o.IsPaid && o.Status != OrderStatus.Cancelled
                        && o.PaidAtUtc != null && o.PaidAtUtc >= start && o.PaidAtUtc < end)
            .Select(o => new
            {
                o.Id,
                o.AccountedAtUtc,
                o.Currency,
                Subtotal = o.Lines.Sum(l => (decimal?)l.UnitPriceSnapshot * l.Quantity) ?? 0m,
                o.DiscountAmount,
                o.RoundingAdjustment
            })
            .ToListAsync(cancellationToken);

        var orderIds = paid.Select(p => p.Id).ToList();

        // Operational takings = net captured payments (charge − refund) across the day's paid orders.
        var byMethod = await _db.Payments
            .Where(p => orderIds.Contains(p.OrderId) && p.Status == PaymentEntryStatus.Captured)
            .GroupBy(p => p.Method)
            .Select(g => new { Method = g.Key, Amount = g.Sum(p => p.Kind == PaymentKind.Charge ? p.Amount : -p.Amount) })
            .ToListAsync(cancellationToken);
        var operational = byMethod.Sum(m => m.Amount);

        // Posted to books = Income (Sales) transactions the Cash Counter import created for the date.
        var occurredOn = request.Date.ToDateTime(TimeOnly.MinValue);
        var nextDay = occurredOn.AddDays(1);
        var posted = await _db.FinanceTransactions
            .Where(t => t.Type == TransactionType.Income && t.OccurredOn >= occurredOn && t.OccurredOn < nextDay)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        // Methods with takings but no active cash account → the import skips them, leaving the day unaccounted.
        var activeKinds = await _db.CashAccounts.Where(a => a.IsActive).Select(a => a.Kind).Distinct().ToListAsync(cancellationToken);
        var blocked = byMethod
            .Where(m => m.Amount > 0m && !activeKinds.Contains(MapKind(m.Method)))
            .Select(m => m.Method.ToString())
            .ToList();

        var unaccounted = paid.Where(p => p.AccountedAtUtc == null).ToList();
        var unaccountedAmount = unaccounted.Sum(u => Math.Max(0m, u.Subtotal - u.DiscountAmount + u.RoundingAdjustment));
        var currency = paid.Select(p => p.Currency).FirstOrDefault() ?? "Tk";
        var variance = operational - posted;

        var reconciled = unaccounted.Count == 0 && blocked.Count == 0 && Math.Abs(variance) < 0.01m;

        return new SalesGlReconciliationDto(
            request.Date, currency, operational, posted, variance,
            unaccounted.Count, unaccountedAmount, blocked, reconciled);
    }

    // Mirrors ImportCashCounterCommand.MapKind — keep in sync.
    private static CashAccountKind MapKind(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => CashAccountKind.Cash,
        PaymentMethod.Mobile => CashAccountKind.MobileWallet,
        PaymentMethod.Card => CashAccountKind.Bank,
        _ => CashAccountKind.Cash
    };
}
