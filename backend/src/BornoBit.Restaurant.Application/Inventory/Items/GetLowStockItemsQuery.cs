using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Items;

/// <summary>Active items at or below their reorder level — the low-stock dashboard.</summary>
public record GetLowStockItemsQuery : IRequest<IReadOnlyList<InventoryItemDto>>;

public class GetLowStockItemsQueryHandler : IRequestHandler<GetLowStockItemsQuery, IReadOnlyList<InventoryItemDto>>
{
    private readonly IAppDbContext _db;

    public GetLowStockItemsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<InventoryItemDto>> Handle(GetLowStockItemsQuery request, CancellationToken cancellationToken)
    {
        return await (
            from i in _db.InventoryItems
            join c in _db.InventoryCategories on i.InventoryCategoryId equals c.Id
            join u in _db.Units on i.BaseUnitId equals u.Id
            where i.IsActive && i.ReorderLevel > 0 && i.QtyOnHand <= i.ReorderLevel
            orderby c.DisplayOrder, i.Name
            select new InventoryItemDto(
                i.Id, i.Code, i.Name, i.BanglaName, i.InventoryCategoryId, c.Name,
                i.ItemType, i.BaseUnitId, u.Code, i.QtyOnHand, i.ReorderLevel, i.ReorderQty,
                i.AvgCost, i.Currency, i.IsPerishable, i.IsActive, i.ProductId, i.PackSize, i.PackNote,
                true, i.QtyOnHand * i.AvgCost))
            .ToListAsync(cancellationToken);
    }
}
