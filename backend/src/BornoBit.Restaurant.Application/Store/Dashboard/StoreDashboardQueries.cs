using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Store;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Store.Dashboard;

// ---- KPI summary ----

public record GetStoreDashboardSummaryQuery : IRequest<StoreDashboardSummaryDto>;

public record StoreDashboardSummaryDto(
    decimal TotalStockValue,
    int ActiveItemCount,
    int LowStockItemCount,
    int DraftGrnCount,
    int DraftIssueCount,
    string Currency);

public class GetStoreDashboardSummaryQueryHandler : IRequestHandler<GetStoreDashboardSummaryQuery, StoreDashboardSummaryDto>
{
    private readonly IAppDbContext _db;
    public GetStoreDashboardSummaryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<StoreDashboardSummaryDto> Handle(GetStoreDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var active = await _db.StoreItems
            .Where(i => i.IsActive)
            .Select(i => new { i.QtyOnHand, i.AvgCost, i.ReorderLevel, i.Currency })
            .ToListAsync(cancellationToken);

        var totalValue = active.Sum(i => i.QtyOnHand * i.AvgCost);
        var lowStock = active.Count(i => i.ReorderLevel > 0 && i.QtyOnHand <= i.ReorderLevel);
        var currency = active.Select(i => i.Currency).FirstOrDefault() ?? "Tk";

        var draftGrn = await _db.StoreGoodsReceipts.CountAsync(g => g.Status == StoreGoodsReceiptStatus.Draft, cancellationToken);
        var draftIssue = await _db.StoreIssues.CountAsync(i => i.Status == StoreIssueStatus.Draft, cancellationToken);

        return new StoreDashboardSummaryDto(totalValue, active.Count, lowStock, draftGrn, draftIssue, currency);
    }
}

// ---- Low-stock rows (dashboard table) ----

public record GetStoreLowStockRowsQuery : IRequest<IReadOnlyList<StoreLowStockRow>>;

public record StoreLowStockRow(
    Guid ItemId,
    string Code,
    string Name,
    string UnitCode,
    decimal QtyOnHand,
    decimal ReorderLevel,
    decimal ReorderQty,
    decimal StockValue);

public class GetStoreLowStockRowsQueryHandler : IRequestHandler<GetStoreLowStockRowsQuery, IReadOnlyList<StoreLowStockRow>>
{
    private readonly IAppDbContext _db;
    public GetStoreLowStockRowsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<StoreLowStockRow>> Handle(GetStoreLowStockRowsQuery request, CancellationToken cancellationToken)
    {
        return await (
            from i in _db.StoreItems
            join u in _db.StoreUnits on i.BaseUnitId equals u.Id
            where i.IsActive && i.ReorderLevel > 0 && i.QtyOnHand <= i.ReorderLevel
            orderby i.QtyOnHand
            select new StoreLowStockRow(
                i.Id, i.Code, i.Name, u.Code,
                i.QtyOnHand, i.ReorderLevel, i.ReorderQty, i.QtyOnHand * i.AvgCost))
            .ToListAsync(cancellationToken);
    }
}
