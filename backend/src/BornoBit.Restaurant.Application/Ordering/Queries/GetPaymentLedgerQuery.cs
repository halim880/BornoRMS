using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Queries;

/// <summary>
/// Paged payment ledger — the real "Paid Today" transaction list, sourced from <see cref="Payment"/> rows
/// (one per tender/refund). Server-side filtered + paged for performance at 500+ transactions/day.
/// </summary>
public record GetPaymentLedgerQuery(
    DateOnly? Date = null,
    PaymentMethod? Method = null,
    PaymentEntryStatus? Status = null,
    string? Cashier = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<PaymentLedgerRowDto>>;

public record PaymentLedgerRowDto(
    Guid PaymentId,
    Guid OrderId,
    string OrderNumber,
    PaymentMethod Method,
    PaymentProvider? Provider,
    PaymentKind Kind,
    decimal Amount,
    decimal SignedAmount,
    DateTime CreatedAtUtc,
    string? CashierName,
    PaymentEntryStatus Status,
    string? Reference);

public class GetPaymentLedgerQueryHandler : IRequestHandler<GetPaymentLedgerQuery, PagedResult<PaymentLedgerRowDto>>
{
    private readonly IAppDbContext _db;
    private readonly IBusinessClock _clock;
    public GetPaymentLedgerQueryHandler(IAppDbContext db, IBusinessClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PagedResult<PaymentLedgerRowDto>> Handle(GetPaymentLedgerQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = from p in _db.Payments
                    join o in _db.Orders on p.OrderId equals o.Id
                    select new { p, o.OrderNumber };

        if (request.Date is { } date)
        {
            var (from, to) = _clock.DayWindowUtc(date);
            query = query.Where(x => x.p.CreatedAtUtc >= from && x.p.CreatedAtUtc < to);
        }
        if (request.Method is { } method) query = query.Where(x => x.p.Method == method);
        if (request.Status is { } status) query = query.Where(x => x.p.Status == status);
        if (!string.IsNullOrWhiteSpace(request.Cashier))
            query = query.Where(x => x.p.CashierName != null && x.p.CashierName.Contains(request.Cashier));

        var total = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PaymentLedgerRowDto(
                x.p.Id,
                x.p.OrderId,
                x.OrderNumber,
                x.p.Method,
                x.p.Provider,
                x.p.Kind,
                x.p.Amount,
                x.p.Status == PaymentEntryStatus.Captured
                    ? (x.p.Kind == PaymentKind.Charge ? x.p.Amount : -x.p.Amount)
                    : 0m,
                x.p.CreatedAtUtc,
                x.p.CashierName,
                x.p.Status,
                x.p.Reference))
            .ToListAsync(cancellationToken);

        return new PagedResult<PaymentLedgerRowDto>(items, page, pageSize, total);
    }
}
