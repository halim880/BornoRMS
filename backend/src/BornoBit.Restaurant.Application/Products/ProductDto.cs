namespace BornoBit.Restaurant.Application.Products;

public record ProductDto(
    Guid Id,
    string Code,
    string Name,
    string? BanglaName,
    Guid ProductCategoryId,
    string CategoryName,
    decimal Price,
    string Currency,
    string? Description,
    string? ImagePath,
    int DisplayOrder,
    bool IsActive);
