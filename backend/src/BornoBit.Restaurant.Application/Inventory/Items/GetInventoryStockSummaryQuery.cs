using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Items;

/// <summary>Aggregate totals for the stock-items grid, computed across the whole filtered set
/// (not just the current page). Filter params must stay identical to <see cref="GetInventoryItemsQuery"/>.</summary>
public record GetInventoryStockSummaryQuery(
    string? Search = null,
    Guid? CategoryId = null,
    InventoryItemType? ItemType = null,
    bool LowStockOnly = false,
    bool IncludeInactive = true
) : IRequest<InventoryStockSummaryDto>;

public record InventoryStockSummaryDto(int ItemCount, decimal TotalStockValue, int LowStockCount);

public class GetInventoryStockSummaryQueryHandler : IRequestHandler<GetInventoryStockSummaryQuery, InventoryStockSummaryDto>
{
    private readonly IAppDbContext _db;

    public GetInventoryStockSummaryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<InventoryStockSummaryDto> Handle(GetInventoryStockSummaryQuery request, CancellationToken cancellationToken)
    {
        var query = _db.InventoryItems.AsQueryable();

        if (!request.IncludeInactive)
            query = query.Where(i => i.IsActive);

        if (request.CategoryId is { } cid)
            query = query.Where(i => i.InventoryCategoryId == cid);

        if (request.ItemType is { } it)
            query = query.Where(i => i.ItemType == it);

        if (request.LowStockOnly)
            query = query.Where(i => i.ReorderLevel > 0 && i.QtyOnHand <= i.ReorderLevel);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(i =>
                EF.Functions.Like(i.Name, $"%{term}%") ||
                EF.Functions.Like(i.Code, $"%{term}%") ||
                (i.BanglaName != null && EF.Functions.Like(i.BanglaName, $"%{term}%")));
        }

        var itemCount = await query.CountAsync(cancellationToken);
        var totalValue = await query.SumAsync(i => (decimal?)(i.QtyOnHand * i.AvgCost), cancellationToken) ?? 0m;
        var lowStockCount = await query.CountAsync(i => i.ReorderLevel > 0 && i.QtyOnHand <= i.ReorderLevel, cancellationToken);

        return new InventoryStockSummaryDto(itemCount, totalValue, lowStockCount);
    }
}
