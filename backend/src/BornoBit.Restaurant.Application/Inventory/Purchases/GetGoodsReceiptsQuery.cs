using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Purchases;

public record GetGoodsReceiptsQuery(
    GoodsReceiptStatus? Status = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<GoodsReceiptListItemDto>>;

public class GetGoodsReceiptsQueryHandler : IRequestHandler<GetGoodsReceiptsQuery, PagedResult<GoodsReceiptListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetGoodsReceiptsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<GoodsReceiptListItemDto>> Handle(GetGoodsReceiptsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query =
            from g in _db.GoodsReceipts
            join s in _db.Suppliers on g.SupplierId equals s.Id
            select new { Grn = g, Supplier = s };

        if (request.Status is { } st)
            query = query.Where(x => x.Grn.Status == st);

        var total = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Grn.ReceivedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new GoodsReceiptListItemDto(
                x.Grn.Id,
                x.Grn.GrnNumber,
                x.Grn.SupplierId,
                x.Supplier.Name,
                x.Grn.InvoiceNo,
                x.Grn.ReceivedAtUtc,
                x.Grn.Currency,
                x.Grn.Status,
                x.Grn.Lines.Count(),
                x.Grn.Lines.Sum(l => (decimal?)l.Qty * l.UnitCost) ?? 0m))
            .ToListAsync(cancellationToken);

        return new PagedResult<GoodsReceiptListItemDto>(items, page, pageSize, total);
    }
}
