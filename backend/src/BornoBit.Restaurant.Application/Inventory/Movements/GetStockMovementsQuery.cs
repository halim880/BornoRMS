using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Movements;

/// <summary>Paged stock ledger, newest first, optionally filtered by item / type / date range.</summary>
public record GetStockMovementsQuery(
    Guid? ItemId = null,
    StockMovementType? MovementType = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<StockMovementDto>>;

public class GetStockMovementsQueryHandler : IRequestHandler<GetStockMovementsQuery, PagedResult<StockMovementDto>>
{
    private readonly IAppDbContext _db;

    public GetStockMovementsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<StockMovementDto>> Handle(GetStockMovementsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query =
            from m in _db.StockMovements
            join i in _db.InventoryItems on m.InventoryItemId equals i.Id
            join u in _db.Units on i.BaseUnitId equals u.Id
            select new { Movement = m, Item = i, Unit = u };

        if (request.ItemId is { } id)
            query = query.Where(x => x.Movement.InventoryItemId == id);

        if (request.MovementType is { } mt)
            query = query.Where(x => x.Movement.MovementType == mt);

        if (request.FromUtc is { } from)
            query = query.Where(x => x.Movement.OccurredAtUtc >= from);

        if (request.ToUtc is { } to)
            query = query.Where(x => x.Movement.OccurredAtUtc < to);

        var total = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Movement.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new StockMovementDto(
                x.Movement.Id,
                x.Movement.InventoryItemId,
                x.Item.Code,
                x.Item.Name,
                x.Unit.Code,
                x.Movement.MovementType,
                x.Movement.QtyBase,
                x.Movement.UnitCost,
                x.Movement.Reason,
                x.Movement.ReferenceType,
                x.Movement.ReferenceId,
                x.Movement.OccurredAtUtc,
                x.Movement.CreatedBy))
            .ToListAsync(cancellationToken);

        return new PagedResult<StockMovementDto>(items, page, pageSize, total);
    }
}
