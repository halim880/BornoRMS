using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Products;

/// <summary>
/// Returns all products (active and inactive) for the admin page, joined to their
/// category name, ordered by DisplayOrder then Name.
/// </summary>
public record GetProductsQuery : IRequest<IReadOnlyList<ProductDto>>;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly IAppDbContext _db;

    public GetProductsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        return await (
            from p in _db.Products
            join c in _db.ProductCategories on p.ProductCategoryId equals c.Id
            orderby p.DisplayOrder, p.Name
            select new ProductDto(
                p.Id, p.Code, p.Name, p.BanglaName, p.ProductCategoryId, c.Name,
                p.Price, p.Currency, p.Description, p.ImagePath, p.DisplayOrder, p.IsActive,
                _db.ProductVariants
                    .Where(v => v.ProductId == p.Id && v.IsActive)
                    .OrderBy(v => v.DisplayOrder).ThenBy(v => v.Name)
                    .Select(v => new ProductVariantDto(v.Id, v.Name, v.Price, v.DisplayOrder))
                    .ToList(),
                p.KitchenStationId,
                p.InventoryMethod))
            .ToListAsync(cancellationToken);
    }
}
