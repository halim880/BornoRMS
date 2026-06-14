using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Catalog;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Recipes;

/// <summary>Lists recipe-based products with their ingredient counts, for the recipe index page.</summary>
public record GetRecipesQuery : IRequest<IReadOnlyList<RecipeListRowDto>>;

public class GetRecipesQueryHandler : IRequestHandler<GetRecipesQuery, IReadOnlyList<RecipeListRowDto>>
{
    private readonly IAppDbContext _db;

    public GetRecipesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<RecipeListRowDto>> Handle(GetRecipesQuery request, CancellationToken cancellationToken)
    {
        return await (
            from p in _db.Products
            where p.InventoryMethod == InventoryMethod.RecipeBased
            join r in _db.Recipes on p.Id equals r.ProductId into rj
            from r in rj.DefaultIfEmpty()
            orderby p.Name
            select new RecipeListRowDto(
                p.Id, p.Code, p.Name,
                r == null ? 1m : r.Yield,
                r == null ? 0 : r.Items.Count,
                r == null ? false : r.IsActive))
            .ToListAsync(cancellationToken);
    }
}
