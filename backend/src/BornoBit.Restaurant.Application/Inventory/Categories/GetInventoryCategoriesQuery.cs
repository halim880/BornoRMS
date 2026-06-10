using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Categories;

public record GetInventoryCategoriesQuery : IRequest<IReadOnlyList<InventoryCategoryDto>>;

public class GetInventoryCategoriesQueryHandler : IRequestHandler<GetInventoryCategoriesQuery, IReadOnlyList<InventoryCategoryDto>>
{
    private readonly IAppDbContext _db;

    public GetInventoryCategoriesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<InventoryCategoryDto>> Handle(GetInventoryCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _db.InventoryCategories
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new InventoryCategoryDto(c.Id, c.Name, c.BanglaName, c.Description, c.DisplayOrder, c.IsActive))
            .ToListAsync(cancellationToken);
    }
}
