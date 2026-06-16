using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Store;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Store.Reports;

public record StoreDepartmentConsumptionItemDto(
    Guid StoreItemId,
    string ItemName,
    string BaseUnitCode,
    decimal QtyBase,
    decimal Value);

public record StoreDepartmentConsumptionRowDto(
    Guid StoreDepartmentId,
    string DepartmentName,
    decimal TotalQtyBase,
    decimal TotalValue,
    IReadOnlyList<StoreDepartmentConsumptionItemDto> Items);

public record StoreDepartmentConsumptionResultDto(
    DateTime FromUtc,
    DateTime ToUtc,
    decimal GrandTotalValue,
    IReadOnlyList<StoreDepartmentConsumptionRowDto> Rows);

/// <summary>
/// Department-wise consumption (what the store issued to each department) over a date range. Computed from the
/// <c>IssueOut</c> stock-movement ledger joined to its posted store issue, so voided issues are excluded and the
/// value uses the cost snapshot captured on each movement at post time.
/// </summary>
public record GetStoreDepartmentConsumptionQuery(
    DateTime FromUtc,
    DateTime ToUtc,
    Guid? StoreDepartmentId = null) : IRequest<StoreDepartmentConsumptionResultDto>;

public class GetStoreDepartmentConsumptionQueryHandler
    : IRequestHandler<GetStoreDepartmentConsumptionQuery, StoreDepartmentConsumptionResultDto>
{
    private readonly IAppDbContext _db;
    public GetStoreDepartmentConsumptionQueryHandler(IAppDbContext db) => _db = db;

    public async Task<StoreDepartmentConsumptionResultDto> Handle(GetStoreDepartmentConsumptionQuery request, CancellationToken cancellationToken)
    {
        var issueRef = nameof(StoreIssue);

        var flat = await (
            from m in _db.StoreStockMovements
            where m.MovementType == StoreMovementType.IssueOut
                  && m.ReferenceType == issueRef
                  && m.OccurredAtUtc >= request.FromUtc
                  && m.OccurredAtUtc < request.ToUtc
            join issue in _db.StoreIssues on m.ReferenceId equals (Guid?)issue.Id
            where issue.Status == StoreIssueStatus.Posted
                  && (request.StoreDepartmentId == null || issue.StoreDepartmentId == request.StoreDepartmentId)
            join dept in _db.StoreDepartments on issue.StoreDepartmentId equals dept.Id
            join item in _db.StoreItems on m.StoreItemId equals item.Id
            join bu in _db.StoreUnits on item.BaseUnitId equals bu.Id
            select new
            {
                DepartmentId = dept.Id,
                DepartmentName = dept.Name,
                ItemId = item.Id,
                item.Name,
                BaseUnitCode = bu.Code,
                Qty = -m.QtyBase,
                Value = -m.QtyBase * m.UnitCost
            })
            .ToListAsync(cancellationToken);

        var rows = flat
            .GroupBy(x => new { x.DepartmentId, x.DepartmentName })
            .OrderBy(g => g.Key.DepartmentName)
            .Select(g => new StoreDepartmentConsumptionRowDto(
                g.Key.DepartmentId,
                g.Key.DepartmentName,
                g.Sum(x => x.Qty),
                g.Sum(x => x.Value),
                g.GroupBy(x => new { x.ItemId, x.Name, x.BaseUnitCode })
                    .OrderBy(ig => ig.Key.Name)
                    .Select(ig => new StoreDepartmentConsumptionItemDto(
                        ig.Key.ItemId, ig.Key.Name, ig.Key.BaseUnitCode,
                        ig.Sum(x => x.Qty), ig.Sum(x => x.Value)))
                    .ToList()))
            .ToList();

        return new StoreDepartmentConsumptionResultDto(
            request.FromUtc, request.ToUtc,
            rows.Sum(r => r.TotalValue),
            rows);
    }
}
