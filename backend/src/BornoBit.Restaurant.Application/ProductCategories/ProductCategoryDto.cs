namespace BornoBit.Restaurant.Application.ProductCategories;

public record ProductCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    int DisplayOrder,
    bool IsActive,
    decimal? TaxRatePercent);
