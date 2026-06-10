namespace BornoBit.Restaurant.Application.Inventory.Categories;

public record InventoryCategoryDto(
    Guid Id,
    string Name,
    string? BanglaName,
    string? Description,
    int DisplayOrder,
    bool IsActive);
