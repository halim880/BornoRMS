using BornoBit.Restaurant.Domain.Inventory;

namespace BornoBit.Restaurant.Application.Inventory.Movements;

public record StockMovementDto(
    Guid Id,
    Guid InventoryItemId,
    string ItemCode,
    string ItemName,
    string UnitCode,
    StockMovementType MovementType,
    decimal QtyBase,
    decimal UnitCost,
    string? Reason,
    string? ReferenceType,
    Guid? ReferenceId,
    DateTime OccurredAtUtc,
    string? CreatedBy);
