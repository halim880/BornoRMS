using BornoBit.Restaurant.Domain.Inventory;

namespace BornoBit.Restaurant.Application.Inventory.Units;

public record UnitDto(
    Guid Id,
    string Code,
    string Name,
    string? BanglaName,
    UnitDimension Dimension,
    decimal ToBaseFactor,
    bool IsActive);
