using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Products;

/// <summary>Returns the modifier / add-on option groups (with options) for one product.</summary>
public record GetProductOptionGroupsQuery(Guid ProductId) : IRequest<IReadOnlyList<ProductOptionGroupDto>>;

public class GetProductOptionGroupsQueryHandler : IRequestHandler<GetProductOptionGroupsQuery, IReadOnlyList<ProductOptionGroupDto>>
{
    private readonly IAppDbContext _db;

    public GetProductOptionGroupsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductOptionGroupDto>> Handle(GetProductOptionGroupsQuery request, CancellationToken cancellationToken)
    {
        return await _db.ProductOptionGroups
            .Where(g => g.ProductId == request.ProductId)
            .OrderBy(g => g.DisplayOrder).ThenBy(g => g.Name)
            .Select(g => new ProductOptionGroupDto(
                g.Id, g.Name, g.BanglaName, g.MinSelections, g.MaxSelections, g.DisplayOrder,
                g.Options
                    .OrderBy(o => o.DisplayOrder).ThenBy(o => o.Name)
                    .Select(o => new ProductOptionDto(o.Id, o.Name, o.BanglaName, o.PriceDelta, o.DisplayOrder))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }
}
