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
    string? Description,
    decimal Price,
    string Currency,
    string? ImageUrl,
    int DisplayOrder);

public class GetMenuQueryHandler : IRequestHandler<GetMenuQuery, IReadOnlyList<MenuCategoryDto>>
{
    private readonly IAppDbContext _db;

    public GetMenuQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<MenuCategoryDto>> Handle(GetMenuQuery request, CancellationToken cancellationToken)
    {
        var categories = await _db.MenuCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new MenuCategoryDto(
                c.Id,
                c.Name,
                c.Description,
                c.DisplayOrder,
                _db.MenuItems
                    .Where(i => i.MenuCategoryId == c.Id && i.IsAvailable)
                    .OrderBy(i => i.DisplayOrder).ThenBy(i => i.Name)
                    .Select(i => new MenuItemDto(
                        i.Id, i.Code, i.Name, i.Description, i.Price, i.Currency, i.ImageUrl, i.DisplayOrder))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return categories;
    }
}
