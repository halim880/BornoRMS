using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Purchases;

public record GetGoodsReceiptQuery(Guid Id) : IRequest<GoodsReceiptDetailDto>;

public class GetGoodsReceiptQueryHandler : IRequestHandler<GetGoodsReceiptQuery, GoodsReceiptDetailDto>
{
    private readonly IAppDbContext _db;

    public GetGoodsReceiptQueryHandler(IAppDbContext db) => _db = db;

    public async Task<GoodsReceiptDetailDto> Handle(GetGoodsReceiptQuery request, CancellationToken cancellationToken)
    {
        var grn = await _db.GoodsReceipts
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Goods receipt {request.Id} not found.");

        var supplierName = await _db.Suppliers
            .Where(s => s.Id == grn.SupplierId)
            .Select(s => s.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? "(unknown)";

        var lines = await (
            from l in _db.GoodsReceiptLines
            join u in _db.Units on l.UnitId equals u.Id
            where l.GoodsReceiptId == grn.Id
            select new GoodsReceiptLineDto(
                l.Id, l.InventoryItemId, l.ItemName, l.Qty, l.UnitId, u.Code, l.QtyBase, l.UnitCost, l.Qty * l.UnitCost))
            .ToListAsync(cancellationToken);

        return new GoodsReceiptDetailDto(
            grn.Id, grn.GrnNumber, grn.SupplierId, supplierName, grn.InvoiceNo, grn.ReceivedAtUtc,
            grn.Currency, grn.Notes, grn.Status, grn.PostedAtUtc, lines.Sum(l => l.LineTotal), lines);
    }
}
