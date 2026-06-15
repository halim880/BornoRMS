using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.PurchaseOrders;

public record GetPurchaseOrderQuery(Guid Id) : IRequest<PurchaseOrderDetailDto>;

public class GetPurchaseOrderQueryHandler : IRequestHandler<GetPurchaseOrderQuery, PurchaseOrderDetailDto>
{
    private readonly IAppDbContext _db;

    public GetPurchaseOrderQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PurchaseOrderDetailDto> Handle(GetPurchaseOrderQuery request, CancellationToken cancellationToken)
    {
        var po = await _db.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Purchase order {request.Id} not found.");

        var supplierName = await _db.Suppliers
            .Where(s => s.Id == po.SupplierId)
            .Select(s => s.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? "(unknown)";

        var lines = await (
            from l in _db.PurchaseOrderLines
            join u in _db.Units on l.UnitId equals u.Id
            where l.PurchaseOrderId == po.Id
            select new PurchaseOrderLineDto(
                l.Id, l.InventoryItemId, l.ItemName, l.QtyOrdered, l.UnitId, u.Code,
                l.QtyOrderedBase, l.QtyReceivedBase,
                l.QtyOrderedBase - l.QtyReceivedBase > 0m ? l.QtyOrderedBase - l.QtyReceivedBase : 0m,
                l.UnitCost, l.QtyOrdered * l.UnitCost))
            .ToListAsync(cancellationToken);

        var receipts = await (
            from g in _db.GoodsReceipts
            where g.PurchaseOrderId == po.Id
            orderby g.ReceivedAtUtc
            select new PurchaseOrderReceiptDto(
                g.Id, g.GrnNumber, g.ReceivedAtUtc, g.Status,
                g.Lines.Sum(l => (decimal?)l.Qty * l.UnitCost) ?? 0m))
            .ToListAsync(cancellationToken);

        return new PurchaseOrderDetailDto(
            po.Id, po.PoNumber, po.SupplierId, supplierName, po.OrderedAtUtc, po.ExpectedAtUtc,
            po.Currency, po.Notes, po.Status, po.ApprovedAtUtc, lines.Sum(l => l.LineTotal), lines, receipts);
    }
}
