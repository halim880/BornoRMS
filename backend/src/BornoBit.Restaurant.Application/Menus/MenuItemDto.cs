namespace BornoBit.Restaurant.Application.Menus;

public record MenuItemDto(
    Guid Id,
    string Title,
    string? Url,
    string? Icon,
    int DisplayOrder,
    string? RequiredRole,
    IReadOnlyList<MenuItemDto> Children
);
