using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Dashboard;

/// <summary>Active items whose projected stock has hit zero or gone negative.</summary>
public record GetOutOfStockQuery : IRequest<IReadOnlyList<OutOfStockRow>>;

public record OutOfStockRow(Guid ItemId, string Code, string Name, string UnitCode, decimal CurrentStock);

public class GetOutOfStockQueryHandler : IRequestHandler<GetOutOfStockQuery, IReadOnlyList<OutOfStockRow>>
{
    private readonly IAppDbContext _db;

    public GetOutOfStockQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<OutOfStockRow>> Handle(GetOutOfStockQuery request, CancellationToken cancellationToken)
    {
        // Available = projection where present, else on-hand cache.
        var rows = await (
            from i in _db.InventoryItems
            join u in _db.Units on i.BaseUnitId equals u.Id
            where i.IsActive
            join p in _db.StockProjections on i.Id equals p.InventoryItemId into pj
            from p in pj.DefaultIfEmpty()
            select new { i.Id, i.Code, i.Name, UnitCode = u.Code, Current = p == null ? i.QtyOnHand : p.CurrentStock })
            .ToListAsync(cancellationToken);

        return rows
            .Where(r => r.Current <= 0m)
            .OrderBy(r => r.Name)
            .Select(r => new OutOfStockRow(r.Id, r.Code, r.Name, r.UnitCode, r.Current))
            .ToList();
    }
}
