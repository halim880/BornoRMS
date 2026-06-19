using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Purchases;

/// <summary>Posted goods receipts over a date range (UTC), grouped by supplier — the
/// "what did we actually buy" report. Only <see cref="GoodsReceiptStatus.Posted"/> receipts
/// are counted, because those are the ones that raised stock and Accounts Payable; drafts have
/// not hit the books. Amounts are Σ(line Qty × UnitCost) in the GRN currency.</summary>
public record GetPurchaseReportQuery(DateTime From, DateTime To) : IRequest<PurchaseReportDto>;

public record PurchaseReportRowDto(Guid SupplierId, string SupplierName, int GrnCount, decimal Subtotal);

public record PurchaseReportDto(
    IReadOnlyList<PurchaseReportRowDto> Rows,
    int TotalGrns,
    decimal GrandTotal,
    string Currency);

public class GetPurchaseReportQueryHandler : IRequestHandler<GetPurchaseReportQuery, PurchaseReportDto>
{
    private readonly IAppDbContext _db;
    private readonly IBusinessClock _clock;

    public GetPurchaseReportQueryHandler(IAppDbContext db, IBusinessClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PurchaseReportDto> Handle(GetPurchaseReportQuery request, CancellationToken cancellationToken)
    {
        var (start, end) = _clock.RangeUtc(DateOnly.FromDateTime(request.From), DateOnly.FromDateTime(request.To));

        var rows = await (
            from g in _db.GoodsReceipts
            join s in _db.Suppliers on g.SupplierId equals s.Id
            where g.Status == GoodsReceiptStatus.Posted
                  && g.ReceivedAtUtc >= start
                  && g.ReceivedAtUtc < end
            select new
            {
                g.SupplierId,
                SupplierName = s.Name,
                g.Currency,
                Subtotal = g.Lines.Sum(l => (decimal?)l.Qty * l.UnitCost) ?? 0m
            })
            .ToListAsync(cancellationToken);

        var bySupplier = rows
            .GroupBy(r => new { r.SupplierId, r.SupplierName })
            .Select(grp => new PurchaseReportRowDto(
                grp.Key.SupplierId,
                grp.Key.SupplierName,
                grp.Count(),
                grp.Sum(x => x.Subtotal)))
            .OrderByDescending(r => r.Subtotal)
            .ToList();

        var currency = rows.Select(r => r.Currency).FirstOrDefault() ?? "Tk";

        return new PurchaseReportDto(
            bySupplier,
            rows.Count,
            bySupplier.Sum(r => r.Subtotal),
            currency);
    }
}
