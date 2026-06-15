using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Products;

public record ProductComboDto(bool IsCombo, IReadOnlyList<ComboComponentDto> Components);

/// <summary>Returns a product's combo flag and components (with member product names).</summary>
public record GetProductComboQuery(Guid ProductId) : IRequest<ProductComboDto>;

public class GetProductComboQueryHandler : IRequestHandler<GetProductComboQuery, ProductComboDto>
{
    private readonly IAppDbContext _db;

    public GetProductComboQueryHandler(IAppDbContext db) => _db = db;

    public async Task<ProductComboDto> Handle(GetProductComboQuery request, CancellationToken cancellationToken)
    {
        var isCombo = await _db.Products
            .Where(p => p.Id == request.ProductId)
            .Select(p => p.IsCombo)
            .FirstOrDefaultAsync(cancellationToken);

        var components = await (
            from c in _db.ComboComponents
            join p in _db.Products on c.ComponentProductId equals p.Id
            where c.ComboProductId == request.ProductId
            orderby c.DisplayOrder
            select new ComboComponentDto(c.Id, c.ComponentProductId, p.Name, c.Quantity, c.DisplayOrder))
            .ToListAsync(cancellationToken);

        return new ProductComboDto(isCombo, components);
    }
}
