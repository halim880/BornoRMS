using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Catalog.Queries;

public record GetMenuQuery() : IRequest<IReadOnlyList<MenuCategoryDto>>;

public record MenuCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    int DisplayOrder,
    IReadOnlyList<MenuItemDto> Items);

public record MenuItemDto(
    Guid Id,
    string Code,
    string Name,
    string? BanglaName,
    string? Description,
    decimal Price,
    string Currency,
    string? ImageUrl,
    int DisplayOrder,
    IReadOnlyList<MenuVariantDto> Variants,
    IReadOnlyList<MenuOptionGroupDto> OptionGroups,
    bool IsCombo = false);

public record MenuVariantDto(Guid Id, string Name, decimal Price, int DisplayOrder);

public record MenuOptionDto(Guid Id, string Name, string? BanglaName, decimal PriceDelta, int DisplayOrder);

public record MenuOptionGroupDto(
    Guid Id,
    string Name,
    string? BanglaName,
    int MinSelections,
    int MaxSelections,
    int DisplayOrder,
    IReadOnlyList<MenuOptionDto> Options);

/// <summary>
/// Customer-facing menu. Reads the same <c>Products</c>/<c>ProductCategories</c> catalog the POS
/// and waiter screens use, so availability toggles take effect everywhere at once.
/// </summary>
public class GetMenuQueryHandler : IRequestHandler<GetMenuQuery, IReadOnlyList<MenuCategoryDto>>
{
    private readonly IAppDbContext _db;

    public GetMenuQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<MenuCategoryDto>> Handle(GetMenuQuery request, CancellationToken cancellationToken)
    {
        var categories = await _db.ProductCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new MenuCategoryDto(
                c.Id,
                c.Name,
                c.Description,
                c.DisplayOrder,
                _db.Products
                    .Where(p => p.ProductCategoryId == c.Id && p.IsActive)
                    .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name)
                    .Select(p => new MenuItemDto(
                        p.Id, p.Code, p.Name, p.BanglaName, p.Description, p.Price, p.Currency,
                        p.ImagePath, p.DisplayOrder,
                        p.Variants
                            .Where(v => v.IsActive)
                            .OrderBy(v => v.DisplayOrder).ThenBy(v => v.Name)
                            .Select(v => new MenuVariantDto(v.Id, v.Name, v.Price, v.DisplayOrder))
                            .ToList(),
                        p.OptionGroups
                            .Where(g => g.IsActive)
                            .OrderBy(g => g.DisplayOrder).ThenBy(g => g.Name)
                            .Select(g => new MenuOptionGroupDto(
                                g.Id, g.Name, g.BanglaName, g.MinSelections, g.MaxSelections, g.DisplayOrder,
                                g.Options
                                    .Where(o => o.IsActive)
                                    .OrderBy(o => o.DisplayOrder).ThenBy(o => o.Name)
                                    .Select(o => new MenuOptionDto(o.Id, o.Name, o.BanglaName, o.PriceDelta, o.DisplayOrder))
                                    .ToList()))
                            .ToList(),
                        p.IsCombo))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return categories;
    }
}
