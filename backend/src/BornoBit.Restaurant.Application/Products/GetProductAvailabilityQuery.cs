using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Catalog;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Products;

/// <summary>On-hand availability for a product the POS can sell directly (InventoryMethod = DirectStock).</summary>
public record ProductAvailabilityDto(Guid ProductId, decimal AvailableStock, bool IsLowStock, bool IsOutOfStock);

/// <summary>
/// Lightweight availability lookup for the POS product grid: only DirectStock products carry a hard
/// on-hand figure (read from the StockProjection, falling back to the item cache). Products with no
/// inventory link or recipe-based deduction are unconstrained here and simply omitted from the result —
/// precise recipe availability is checked at order time by GetOrderStockAvailabilityQuery.
/// </summary>
public record GetProductAvailabilityQuery : IRequest<IReadOnlyList<ProductAvailabilityDto>>;

public class GetProductAvailabilityQueryHandler
    : IRequestHandler<GetProductAvailabilityQuery, IReadOnlyList<ProductAvailabilityDto>>
{
    private readonly IAppDbContext _db;

    public GetProductAvailabilityQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductAvailabilityDto>> Handle(
        GetProductAvailabilityQuery request, CancellationToken cancellationToken)
    {
        return await (
            from p in _db.Products
            where p.InventoryMethod == InventoryMethod.DirectStock
            join i in _db.InventoryItems on p.Id equals i.ProductId
            join sp in _db.StockProjections on i.Id equals sp.InventoryItemId into spj
            from sp in spj.DefaultIfEmpty()
            let qty = sp != null ? sp.CurrentStock : i.QtyOnHand
            select new ProductAvailabilityDto(
                p.Id,
                qty,
                i.ReorderLevel > 0 && qty <= i.ReorderLevel,
                qty <= 0m))
            .ToListAsync(cancellationToken);
    }
}
