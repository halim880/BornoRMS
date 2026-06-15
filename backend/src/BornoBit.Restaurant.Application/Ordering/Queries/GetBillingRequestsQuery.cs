using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Queries;

/// <summary>
/// The cashier's "bill requests" queue — every session a waiter has flagged for settlement
/// (Status == Billing). Running bill and balance due are summed from each session's non-cancelled
/// orders (order totals are not EF-translatable, so raw fields are projected then computed in memory).
/// Oldest request first.
/// </summary>
public record GetBillingRequestsQuery : IRequest<IReadOnlyList<BillingRequestRowDto>>;

public record BillingRequestRowDto(
    Guid SessionId,
    string SessionNumber,
    string? TableNumber,
    string? WaiterName,
    int GuestCount,
    int OrderCount,
    decimal RunningBill,
    decimal BalanceDue,
    DateTime RequestedAtUtc,
    string Currency);

public class GetBillingRequestsQueryHandler : IRequestHandler<GetBillingRequestsQuery, IReadOnlyList<BillingRequestRowDto>>
{
    private readonly IAppDbContext _db;
    public GetBillingRequestsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<BillingRequestRowDto>> Handle(GetBillingRequestsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await _db.DiningSessions
            .Where(s => s.Status == DiningSessionStatus.Billing)
            .Select(s => new
            {
                s.Id,
                s.SessionNumber,
                s.WaiterName,
                s.GuestCount,
                s.LastActivityAtUtc,
                TableNumber = _db.RestaurantTables.Where(t => t.Id == s.RestaurantTableId).Select(t => t.TableNumber).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        if (sessions.Count == 0) return Array.Empty<BillingRequestRowDto>();

        var sessionIds = sessions.Select(s => s.Id).ToList();

        var orders = await _db.Orders
            .Where(o => o.DiningSessionId != null && sessionIds.Contains(o.DiningSessionId.Value)
                        && o.Status != OrderStatus.Cancelled)
            .Select(o => new
            {
                SessionId = o.DiningSessionId!.Value,
                o.IsPaid,
                Subtotal = o.Lines.Sum(l => (decimal?)l.UnitPriceSnapshot * l.Quantity) ?? 0m,
                o.DiscountAmount,
                o.TaxAmount,
                o.ServiceChargeAmount,
                o.TipAmount,
                o.RoundingAdjustment,
                o.Currency
            })
            .ToListAsync(cancellationToken);
        var ordersBySession = orders.GroupBy(o => o.SessionId).ToDictionary(g => g.Key, g => g.ToList());

        static decimal Grand(decimal subtotal, decimal discount, decimal tax, decimal service, decimal tip, decimal rounding) =>
            Math.Max(0m, subtotal - discount + tax + service + tip + rounding);

        return sessions
            .Select(s =>
            {
                ordersBySession.TryGetValue(s.Id, out var os);
                os ??= new();

                var running = os.Sum(o => Grand(o.Subtotal, o.DiscountAmount, o.TaxAmount, o.ServiceChargeAmount, o.TipAmount, o.RoundingAdjustment));
                var balance = os.Where(o => !o.IsPaid)
                    .Sum(o => Grand(o.Subtotal, o.DiscountAmount, o.TaxAmount, o.ServiceChargeAmount, o.TipAmount, o.RoundingAdjustment));
                var currency = os.Select(o => o.Currency).FirstOrDefault() ?? "Tk";

                return new BillingRequestRowDto(
                    s.Id, s.SessionNumber, s.TableNumber, s.WaiterName, s.GuestCount,
                    os.Count, running, balance, s.LastActivityAtUtc, currency);
            })
            .OrderBy(r => r.RequestedAtUtc)
            .ToList();
    }
}
