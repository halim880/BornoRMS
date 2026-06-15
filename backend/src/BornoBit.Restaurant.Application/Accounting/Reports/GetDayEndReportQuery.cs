using BornoBit.Restaurant.Application.Accounting.Drawers;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Reports;

/// <summary>
/// A Petpooja-style "day close" for one business day: sales, the payment-method breakdown, drawer
/// shifts with their variance, the day's expenses, and any takings not yet imported into the books.
/// One screen a manager can reconcile against the cash on hand at end of shift.
/// </summary>
public record GetDayEndReportQuery(DateOnly Date) : IRequest<DayEndReportDto>;

public record DayEndReportDto(
    DateOnly Date,
    string Currency,
    int OrderCount,
    decimal SalesSubtotal,
    decimal SalesDiscount,
    decimal SalesTotal,
    IReadOnlyList<DrawerMethodLineDto> ByMethod,
    decimal TotalCollected,
    IReadOnlyList<DrawerDto> Drawers,
    decimal DrawerVariance,
    IReadOnlyList<PlLineDto> Expenses,
    decimal TotalExpenses,
    int UnaccountedOrders,
    decimal UnaccountedAmount);

public class GetDayEndReportQueryHandler : IRequestHandler<GetDayEndReportQuery, DayEndReportDto>
{
    private readonly IAppDbContext _db;
    private readonly IBusinessClock _clock;

    public GetDayEndReportQueryHandler(IAppDbContext db, IBusinessClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<DayEndReportDto> Handle(GetDayEndReportQuery request, CancellationToken cancellationToken)
    {
        var (start, end) = _clock.DayWindowUtc(request.Date);

        // Paid orders for the day. Order.Total is a C# computed prop EF can't translate — recompute inline
        // (mirrors GetSalesReportQuery / GetCollectionReportQuery).
        var paid = await _db.Orders
            .Where(o => o.IsPaid
                        && o.Status != OrderStatus.Cancelled
                        && o.PaidAtUtc != null
                        && o.PaidAtUtc >= start
                        && o.PaidAtUtc < end)
            .Select(o => new
            {
                o.Id,
                Subtotal = o.Lines.Sum(l => (decimal?)l.UnitPriceSnapshot * l.Quantity) ?? 0m,
                o.DiscountAmount,
                o.RoundingAdjustment,
                o.AccountedAtUtc,
                o.Currency
            })
            .ToListAsync(cancellationToken);

        var orderIds = paid.Select(p => p.Id).ToList();

        // Net captured takings per method from the payment ledger (reflects split/partial + refunds).
        var byMethod = await _db.Payments
            .Where(p => orderIds.Contains(p.OrderId) && p.Status == PaymentEntryStatus.Captured)
            .GroupBy(p => p.Method)
            .Select(g => new DrawerMethodLineDto(
                g.Key,
                g.Count(),
                g.Sum(p => p.Kind == PaymentKind.Charge ? p.Amount : -p.Amount)))
            .OrderBy(l => l.Method)
            .ToListAsync(cancellationToken);

        // Drawer shifts opened during the day, with their account name for display.
        var drawerRows = await (
            from d in _db.CashDrawerSessions
            where d.OpenedAtUtc >= start && d.OpenedAtUtc < end
            join a in _db.CashAccounts on d.CashAccountId equals a.Id into acc
            from a in acc.DefaultIfEmpty()
            orderby d.OpenedAtUtc
            select new { d, AccountName = a != null ? a.Name : null })
            .ToListAsync(cancellationToken);
        var drawers = drawerRows.Select(r => r.d.ToDto(r.AccountName)).ToList();

        // Expenses booked on the day, grouped by category. FinanceTransaction.OccurredOn is stored
        // date-at-midnight (see ImportCashCounterCommand), so a half-open day window matches it.
        var dayStart = request.Date.ToDateTime(TimeOnly.MinValue);
        var nextDay = dayStart.AddDays(1);
        var expenses = await (
            from t in _db.FinanceTransactions
            where t.Type == TransactionType.Expense && t.OccurredOn >= dayStart && t.OccurredOn < nextDay
            join c in _db.FinanceCategories on t.CategoryId equals c.Id
            group t by new { t.CategoryId, c.Name } into g
            select new PlLineDto(g.Key.CategoryId, g.Key.Name, g.Sum(x => x.Amount)))
            .ToListAsync(cancellationToken);

        var unaccounted = paid.Where(p => p.AccountedAtUtc == null).ToList();

        var currency = paid.Select(p => p.Currency).FirstOrDefault() ?? "Tk";

        return new DayEndReportDto(
            request.Date,
            currency,
            paid.Count,
            paid.Sum(p => p.Subtotal),
            paid.Sum(p => p.DiscountAmount),
            paid.Sum(p => Math.Max(0m, p.Subtotal - p.DiscountAmount + p.RoundingAdjustment)),
            byMethod,
            byMethod.Sum(m => m.Amount),
            drawers,
            drawers.Where(d => d.Status == DrawerStatus.Closed).Sum(d => d.Variance),
            expenses.OrderByDescending(e => e.Amount).ToList(),
            expenses.Sum(e => e.Amount),
            unaccounted.Count,
            unaccounted.Sum(u => Math.Max(0m, u.Subtotal - u.DiscountAmount + u.RoundingAdjustment)));
    }
}
