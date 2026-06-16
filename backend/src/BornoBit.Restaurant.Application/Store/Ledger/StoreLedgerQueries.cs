using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Store;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Store.Ledger;

public record StoreMovementRow(
    DateTime OccurredAtUtc,
    string ItemName,
    string UnitCode,
    StoreMovementType MovementType,
    decimal QtyBase,
    decimal UnitCost,
    string? Reason,
    string? ReferenceType,
    decimal? RunningBalance);

public record StoreMovementLedgerDto(
    Guid? ItemId,
    string? ItemName,
    string? UnitCode,
    DateTime? FromUtc,
    DateTime? ToUtc,
    decimal? OpeningBalance,
    decimal? ClosingBalance,
    IReadOnlyList<StoreMovementRow> Rows);

/// <summary>
/// Movement ledger for the store. When <see cref="ItemId"/> is set, rows carry a running balance
/// (seeded with the opening balance of movements before <see cref="FromUtc"/>).
/// </summary>
public record GetStoreMovementLedgerQuery(
    Guid? ItemId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Take = 1000) : IRequest<StoreMovementLedgerDto>;

public class GetStoreMovementLedgerQueryHandler : IRequestHandler<GetStoreMovementLedgerQuery, StoreMovementLedgerDto>
{
    private readonly IAppDbContext _db;
    public GetStoreMovementLedgerQueryHandler(IAppDbContext db) => _db = db;

    public async Task<StoreMovementLedgerDto> Handle(GetStoreMovementLedgerQuery request, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 5000);

        var baseQuery = _db.StoreStockMovements.AsQueryable();
        if (request.ItemId is { } id) baseQuery = baseQuery.Where(m => m.StoreItemId == id);
        if (request.FromUtc is { } f) baseQuery = baseQuery.Where(m => m.OccurredAtUtc >= f);
        if (request.ToUtc is { } t) baseQuery = baseQuery.Where(m => m.OccurredAtUtc < t);

        var raw = await (
            from m in baseQuery
            join i in _db.StoreItems on m.StoreItemId equals i.Id
            join u in _db.StoreUnits on i.BaseUnitId equals u.Id
            orderby m.OccurredAtUtc, m.Id
            select new
            {
                m.OccurredAtUtc, ItemName = i.Name, UnitCode = u.Code,
                m.MovementType, m.QtyBase, m.UnitCost, m.Reason, m.ReferenceType
            })
            .Take(take)
            .ToListAsync(cancellationToken);

        // Single-item mode → opening balance + running balance.
        decimal? opening = null;
        decimal? closing = null;
        string? itemName = null, unitCode = null;

        if (request.ItemId is { } itemId)
        {
            var openingQuery = _db.StoreStockMovements.Where(m => m.StoreItemId == itemId);
            if (request.FromUtc is { } f2) openingQuery = openingQuery.Where(m => m.OccurredAtUtc < f2);
            opening = await openingQuery.SumAsync(m => (decimal?)m.QtyBase, cancellationToken) ?? 0m;

            var running = opening.Value;
            var withRunning = new List<StoreMovementRow>(raw.Count);
            foreach (var r in raw)
            {
                running += r.QtyBase;
                withRunning.Add(new StoreMovementRow(r.OccurredAtUtc, r.ItemName, r.UnitCode, r.MovementType,
                    r.QtyBase, r.UnitCost, r.Reason, r.ReferenceType, running));
            }
            closing = running;
            itemName = raw.Count > 0 ? raw[0].ItemName : null;
            unitCode = raw.Count > 0 ? raw[0].UnitCode : null;

            return new StoreMovementLedgerDto(itemId, itemName, unitCode, request.FromUtc, request.ToUtc, opening, closing, withRunning);
        }

        var rows = raw.Select(r => new StoreMovementRow(r.OccurredAtUtc, r.ItemName, r.UnitCode, r.MovementType,
            r.QtyBase, r.UnitCost, r.Reason, r.ReferenceType, null)).ToList();

        return new StoreMovementLedgerDto(null, null, null, request.FromUtc, request.ToUtc, null, null, rows);
    }
}
