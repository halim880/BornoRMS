using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Sessions;

/// <summary>The ready-to-serve queue: orders cooked and waiting to be carried out, oldest first.</summary>
public record GetReadyToServeQuery : IRequest<IReadOnlyList<ReadyToServeRowDto>>;

public class GetReadyToServeQueryHandler : IRequestHandler<GetReadyToServeQuery, IReadOnlyList<ReadyToServeRowDto>>
{
    private readonly IAppDbContext _db;
    public GetReadyToServeQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ReadyToServeRowDto>> Handle(GetReadyToServeQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var orders = await _db.Orders
            .Where(o => o.Status == OrderStatus.Ready)
            .OrderBy(o => o.ReadyAtUtc)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.RestaurantTableId,
                o.DiningSessionId,
                o.ReadyAtUtc,
                Lines = o.Lines.Select(l => new { l.Name, l.Quantity, l.StationName }).ToList()
            })
            .ToListAsync(cancellationToken);

        var tableIds = orders.Where(o => o.RestaurantTableId != null).Select(o => o.RestaurantTableId!.Value).Distinct().ToList();
        var tableNumbers = await _db.RestaurantTables
            .Where(t => tableIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.TableNumber, cancellationToken);

        return orders.Select(o => new ReadyToServeRowDto(
            o.Id, o.OrderNumber, o.RestaurantTableId,
            o.RestaurantTableId is { } tid && tableNumbers.TryGetValue(tid, out var tn) ? tn : null,
            o.DiningSessionId, o.ReadyAtUtc,
            (int)Math.Max(0, (now - (o.ReadyAtUtc ?? now)).TotalMinutes),
            o.Lines.Select(l => new ReadyToServeLineDto(l.Name, l.Quantity, l.StationName)).ToList()))
            .ToList();
    }
}
