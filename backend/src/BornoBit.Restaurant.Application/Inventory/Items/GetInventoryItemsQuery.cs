using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Items;

/// <summary>Paged list of stock items joined to category + base unit, for the admin grid.</summary>
public record GetInventoryItemsQuery(
    string? Search = null,
    Guid? CategoryId = null,
    InventoryItemType? ItemType = null,
    bool LowStockOnly = false,
    bool IncludeInactive = true,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<InventoryItemDto>>;

public class GetInventoryItemsQueryHandler : IRequestHandler<GetInventoryItemsQuery, PagedResult<InventoryItemDto>>
{
    private readonly IAppDbContext _db;

    public GetInventoryItemsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<InventoryItemDto>> Handle(GetInventoryItemsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query =
            from i in _db.InventoryItems
            join c in _db.InventoryCategories on i.InventoryCategoryId equals c.Id
            join u in _db.Units on i.BaseUnitId equals u.Id
            select new { Item = i, Category = c, Unit = u };

        if (!request.IncludeInactive)
            query = query.Where(x => x.Item.IsActive);

        if (request.CategoryId is { } cid)
            query = query.Where(x => x.Item.InventoryCategoryId == cid);

        if (request.ItemType is { } it)
            query = query.Where(x => x.Item.ItemType == it);

        if (request.LowStockOnly)
            query = query.Where(x => x.Item.ReorderLevel > 0 && x.Item.QtyOnHand <= x.Item.ReorderLevel);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x =>
                EF.Functions.Like(x.Item.Name, $"%{term}%") ||
                EF.Functions.Like(x.Item.Code, $"%{term}%") ||
                (x.Item.BanglaName != null && EF.Functions.Like(x.Item.BanglaName, $"%{term}%")));
        }

        var total = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Category.DisplayOrder).ThenBy(x => x.Item.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new InventoryItemDto(
                x.Item.Id,
                x.Item.Code,
                x.Item.Name,
                x.Item.BanglaName,
                x.Item.InventoryCategoryId,
                x.Category.Name,
                x.Item.ItemType,
                x.Item.BaseUnitId,
                x.Unit.Code,
                x.Item.QtyOnHand,
                x.Item.ReorderLevel,
                x.Item.ReorderQty,
                x.Item.AvgCost,
                x.Item.Currency,
                x.Item.IsPerishable,
                x.Item.IsActive,
                x.Item.ProductId,
                x.Item.PackSize,
                x.Item.PackNote,
                x.Item.ReorderLevel > 0 && x.Item.QtyOnHand <= x.Item.ReorderLevel,
                x.Item.QtyOnHand * x.Item.AvgCost))
            .ToListAsync(cancellationToken);

        return new PagedResult<InventoryItemDto>(items, page, pageSize, total);
    }
}
