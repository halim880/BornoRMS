using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Reports;

/// <summary>
/// Output-VAT collected over a date range, grouped by rate — the BD VAT return summary. Built from the
/// frozen per-line tax snapshots stamped at settlement (so historical rates are preserved even if a
/// category's rate later changes). Taxable sales are net of VAT and of order-level discount.
/// </summary>
public record GetVatReportQuery(DateTime From, DateTime To) : IRequest<VatReportDto>;

public record VatReportRowDto(decimal RatePercent, decimal TaxableSales, decimal Vat);

public record VatReportDto(
    IReadOnlyList<VatReportRowDto> Rows,
    decimal TotalTaxable,
    decimal TotalVat,
    string Currency);

public class GetVatReportQueryHandler : IRequestHandler<GetVatReportQuery, VatReportDto>
{
    private readonly IAppDbContext _db;
    private readonly IBusinessClock _clock;

    public GetVatReportQueryHandler(IAppDbContext db, IBusinessClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<VatReportDto> Handle(GetVatReportQuery request, CancellationToken cancellationToken)
    {
        var (start, end) = _clock.RangeUtc(DateOnly.FromDateTime(request.From), DateOnly.FromDateTime(request.To));

        var rows = await (
            from l in _db.OrderLines
            join o in _db.Orders on l.OrderId equals o.Id
            where o.IsPaid
                  && o.Status != OrderStatus.Cancelled
                  && o.PaidAtUtc != null
                  && o.PaidAtUtc >= start
                  && o.PaidAtUtc < end
            group l by l.TaxRatePercentSnapshot into g
            select new VatReportRowDto(
                g.Key,
                g.Sum(x => x.TaxableAmountSnapshot),
                g.Sum(x => x.TaxAmountSnapshot)))
            .ToListAsync(cancellationToken);

        var ordered = rows.OrderBy(r => r.RatePercent).ToList();

        var currency = await _db.RestaurantBillingSettings
            .Select(s => s.Currency)
            .FirstOrDefaultAsync(cancellationToken) ?? "Tk";

        return new VatReportDto(
            ordered,
            ordered.Sum(r => r.TaxableSales),
            ordered.Sum(r => r.Vat),
            currency);
    }
}
