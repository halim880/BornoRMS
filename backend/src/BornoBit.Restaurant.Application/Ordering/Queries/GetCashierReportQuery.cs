using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Queries;

/// <summary>Captured tenders over a date range (UTC), grouped by the cashier who took them.
/// Uses <see cref="Payment.CreatedAtUtc"/> (when the tender was captured) and only counts
/// <see cref="PaymentEntryStatus.Captured"/> rows — voided/refunded-out entries drop out.
/// Charges are money in, refunds are money paid back; Net = Charges − Refunds.</summary>
public record GetCashierReportQuery(DateTime From, DateTime To) : IRequest<CashierReportDto>;

public record CashierReportRowDto(string Cashier, int TxnCount, decimal Charges, decimal Refunds, decimal Net);

public record CashierReportDto(
    IReadOnlyList<CashierReportRowDto> Rows,
    int TotalTxns,
    decimal TotalCharges,
    decimal TotalRefunds,
    decimal TotalNet,
    string Currency);

public class GetCashierReportQueryHandler : IRequestHandler<GetCashierReportQuery, CashierReportDto>
{
    private readonly IAppDbContext _db;
    private readonly IBusinessClock _clock;

    public GetCashierReportQueryHandler(IAppDbContext db, IBusinessClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<CashierReportDto> Handle(GetCashierReportQuery request, CancellationToken cancellationToken)
    {
        var (start, end) = _clock.RangeUtc(DateOnly.FromDateTime(request.From), DateOnly.FromDateTime(request.To));

        var rows = await _db.Payments
            .Where(p => p.Status == PaymentEntryStatus.Captured
                        && p.CreatedAtUtc >= start
                        && p.CreatedAtUtc < end)
            .Select(p => new
            {
                p.CashierName,
                p.Kind,
                p.Amount
            })
            .ToListAsync(cancellationToken);

        var byCashier = rows
            .GroupBy(p => string.IsNullOrWhiteSpace(p.CashierName) ? "Unknown" : p.CashierName!)
            .Select(g => new CashierReportRowDto(
                g.Key,
                g.Count(),
                g.Where(x => x.Kind == PaymentKind.Charge).Sum(x => x.Amount),
                g.Where(x => x.Kind == PaymentKind.Refund).Sum(x => x.Amount),
                g.Where(x => x.Kind == PaymentKind.Charge).Sum(x => x.Amount)
                    - g.Where(x => x.Kind == PaymentKind.Refund).Sum(x => x.Amount)))
            .OrderByDescending(r => r.Net)
            .ToList();

        var currency = await _db.Orders.Select(o => o.Currency).FirstOrDefaultAsync(cancellationToken) ?? "Tk";

        return new CashierReportDto(
            byCashier,
            rows.Count,
            byCashier.Sum(r => r.Charges),
            byCashier.Sum(r => r.Refunds),
            byCashier.Sum(r => r.Net),
            currency);
    }
}
