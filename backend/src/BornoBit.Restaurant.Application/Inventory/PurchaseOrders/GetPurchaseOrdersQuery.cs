using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.PurchaseOrders;

public record GetPurchaseOrdersQuery(
    PurchaseOrderStatus? Status = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<PurchaseOrderListItemDto>>;

public class GetPurchaseOrdersQueryHandler : IRequestHandler<GetPurchaseOrdersQuery, PagedResult<PurchaseOrderListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetPurchaseOrdersQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<PurchaseOrderListItemDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query =
            from p in _db.PurchaseOrders
            join s in _db.Suppliers on p.SupplierId equals s.Id
            select new { Po = p, Supplier = s };

        if (request.Status is { } st)
            query = query.Where(x => x.Po.Status == st);

        var total = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Po.OrderedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PurchaseOrderListItemDto(
                x.Po.Id,
                x.Po.PoNumber,
                x.Po.SupplierId,
                x.Supplier.Name,
                x.Po.OrderedAtUtc,
                x.Po.ExpectedAtUtc,
                x.Po.Currency,
                x.Po.Status,
                x.Po.Lines.Count(),
                x.Po.Lines.Sum(l => (decimal?)l.QtyOrdered * l.UnitCost) ?? 0m))
            .ToListAsync(cancellationToken);

        return new PagedResult<PurchaseOrderListItemDto>(items, page, pageSize, total);
    }
}
