using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.ProductCategories;

/// <summary>
/// Returns all product categories (active and inactive) for the admin page,
/// ordered by DisplayOrder then Name.
/// </summary>
public record GetProductCategoriesQuery : IRequest<IReadOnlyList<ProductCategoryDto>>;

public class GetProductCategoriesQueryHandler : IRequestHandler<GetProductCategoriesQuery, IReadOnlyList<ProductCategoryDto>>
{
    private readonly IAppDbContext _db;

    public GetProductCategoriesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductCategoryDto>> Handle(GetProductCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _db.ProductCategories
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new ProductCategoryDto(c.Id, c.Name, c.Description, c.DisplayOrder, c.IsActive, c.TaxRatePercent))
            .ToListAsync(cancellationToken);
    }
}
