using BornoBit.Restaurant.Domain.Inventory;

namespace BornoBit.Restaurant.Application.Inventory.Items;

public record InventoryItemDto(
    Guid Id,
    string Code,
    string Name,
    string? BanglaName,
    Guid InventoryCategoryId,
    string CategoryName,
    InventoryItemType ItemType,
    Guid BaseUnitId,
    string UnitCode,
    decimal QtyOnHand,
    decimal ReorderLevel,
    decimal ReorderQty,
    decimal AvgCost,
    string Currency,
    bool IsPerishable,
    bool IsActive,
    Guid? ProductId,
    decimal? PackSize,
    string? PackNote,
    bool IsLowStock,
    decimal StockValue);
