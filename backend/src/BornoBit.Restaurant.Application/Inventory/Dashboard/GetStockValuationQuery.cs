using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Dashboard;

/// <summary>Total stock value (Σ QtyOnHand × AvgCost) overall and broken down by category.</summary>
public record GetStockValuationQuery : IRequest<StockValuationDto>;

public record CategoryValueRow(Guid CategoryId, string CategoryName, decimal Value);
public record StockValuationDto(decimal TotalValue, IReadOnlyList<CategoryValueRow> ByCategory);

public class GetStockValuationQueryHandler : IRequestHandler<GetStockValuationQuery, StockValuationDto>
{
    private readonly IAppDbContext _db;

    public GetStockValuationQueryHandler(IAppDbContext db) => _db = db;

    public async Task<StockValuationDto> Handle(GetStockValuationQuery request, CancellationToken cancellationToken)
    {
        var rows = await (
            from i in _db.InventoryItems
            join c in _db.InventoryCategories on i.InventoryCategoryId equals c.Id
            where i.IsActive
            select new { c.Id, c.Name, Value = i.QtyOnHand * i.AvgCost })
            .ToListAsync(cancellationToken);

        var byCategory = rows
            .GroupBy(r => new { r.Id, r.Name })
            .Select(g => new CategoryValueRow(g.Key.Id, g.Key.Name, g.Sum(x => x.Value)))
            .OrderByDescending(r => r.Value)
            .ToList();

        return new StockValuationDto(rows.Sum(r => r.Value), byCategory);
    }
}
