using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Queries;

/// <summary>KPI roll-up for the cash-counter dashboard for one day, sourced from payments + orders.</summary>
public record GetDailySummaryQuery(DateOnly Date) : IRequest<DailySummaryDto>;

public record DailySummaryDto(
    decimal TotalSales,
    decimal CashCollected,
    decimal DigitalPayments,
    decimal DiscountsGiven,
    decimal TaxCollected,
    decimal ServiceCharge,
    decimal Tips,
    decimal RefundsTotal,
    decimal NetRevenue,
    decimal PendingPayments,
    int PendingOrders);

public class GetDailySummaryQueryHandler : IRequestHandler<GetDailySummaryQuery, DailySummaryDto>
{
    private readonly IAppDbContext _db;
    public GetDailySummaryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<DailySummaryDto> Handle(GetDailySummaryQuery request, CancellationToken cancellationToken)
    {
        var from = request.Date.ToDateTime(TimeOnly.MinValue);
        var to = from.AddDays(1);

        // Captured payments today, split charge/refund and cash/digital.
        var payments = await _db.Payments
            .Where(p => p.Status == PaymentEntryStatus.Captured && p.CreatedAtUtc >= from && p.CreatedAtUtc < to)
            .Select(p => new { p.Method, p.Kind, p.Amount })
            .ToListAsync(cancellationToken);

        decimal CashSum(PaymentKind kind) => payments.Where(x => x.Kind == kind && x.Method == PaymentMethod.Cash).Sum(x => x.Amount);
        decimal DigitalSum(PaymentKind kind) => payments.Where(x => x.Kind == kind && x.Method != PaymentMethod.Cash).Sum(x => x.Amount);

        var cashCharges = CashSum(PaymentKind.Charge);
        var cashRefunds = CashSum(PaymentKind.Refund);
        var digitalCharges = DigitalSum(PaymentKind.Charge);
        var digitalRefunds = DigitalSum(PaymentKind.Refund);

        var grossCharges = cashCharges + digitalCharges;
        var refundsTotal = cashRefunds + digitalRefunds;
        var cashCollected = cashCharges - cashRefunds;
        var digitalPayments = digitalCharges - digitalRefunds;

        // Orders that reached Paid today — basis for discount / tax / service / tip figures.
        var paidAgg = await _db.Orders
            .Where(o => o.PaidAtUtc != null && o.PaidAtUtc >= from && o.PaidAtUtc < to)
            .GroupBy(o => 1)
            .Select(g => new
            {
                Discounts = g.Sum(o => o.DiscountAmount),
                Tax = g.Sum(o => o.TaxAmount),
                Service = g.Sum(o => o.ServiceChargeAmount),
                Tips = g.Sum(o => o.TipAmount)
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Outstanding balances for today's not-fully-paid orders.
        var pending = await _db.Orders
            .Where(o => o.OrderedAtUtc >= from && o.OrderedAtUtc < to
                && (o.PaymentStatus == PaymentStatus.Pending || o.PaymentStatus == PaymentStatus.PartiallyPaid))
            .Select(o => new
            {
                Grand = o.Lines.Sum(l => l.UnitPriceSnapshot * l.Quantity)
                        - o.DiscountAmount + o.TaxAmount + o.ServiceChargeAmount + o.TipAmount + o.RoundingAdjustment,
                Paid = _db.Payments.Where(p => p.OrderId == o.Id && p.Status == PaymentEntryStatus.Captured)
                        .Sum(p => p.Kind == PaymentKind.Charge ? p.Amount : -p.Amount)
            })
            .ToListAsync(cancellationToken);

        var pendingAmount = pending.Sum(x => Math.Max(0m, x.Grand - x.Paid));

        return new DailySummaryDto(
            TotalSales: grossCharges,
            CashCollected: cashCollected,
            DigitalPayments: digitalPayments,
            DiscountsGiven: paidAgg?.Discounts ?? 0m,
            TaxCollected: paidAgg?.Tax ?? 0m,
            ServiceCharge: paidAgg?.Service ?? 0m,
            Tips: paidAgg?.Tips ?? 0m,
            RefundsTotal: refundsTotal,
            NetRevenue: grossCharges - refundsTotal,
            PendingPayments: pendingAmount,
            PendingOrders: pending.Count);
    }
}
