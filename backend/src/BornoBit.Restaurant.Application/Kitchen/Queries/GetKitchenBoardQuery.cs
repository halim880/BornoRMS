using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Kitchen.Queries;

/// <summary>
/// The live kitchen board: open orders grouped into Pending / Preparing / Ready columns, optionally
/// scoped to a single kitchen and/or station. A line's kitchen is derived transitively from its station
/// (station-less lines, and stations with no kitchen, fall back to the default kitchen). When a kitchen
/// (or station) is selected an order appears only if it has a matching line, and its item list is filtered
/// to those lines.
/// </summary>
public record GetKitchenBoardQuery(
    Guid? KitchenId = null,
    Guid? StationId = null,
    OrderType? Type = null,
    OrderStatus? Status = null,
    string? TableNumber = null,
    string? SearchOrderNumber = null,
    DateOnly? Date = null) : IRequest<KitchenBoardDto>;

public record KitchenBoardDto(
    IReadOnlyList<KitchenOrderCardDto> Pending,
    IReadOnlyList<KitchenOrderCardDto> Preparing,
    IReadOnlyList<KitchenOrderCardDto> Ready);

public record KitchenOrderCardDto(
    Guid Id,
    string OrderNumber,
    OrderType OrderType,
    OrderStatus Status,
    string? TableNumber,
    string? CustomerName,
    DateTime OrderedAtUtc,
    DateTime? PreparingAtUtc,
    DateTime? ReadyAtUtc,
    bool IsPriority,
    string? KitchenNotes,
    string? CustomerNotes,
    string Source,
    int ItemCount,
    IReadOnlyList<KitchenItemDto> Items);

public record KitchenItemDto(int Quantity, string Name, string? Notes, Guid? StationId, string? StationName, Guid? KitchenId, string? KitchenName);

public class GetKitchenBoardQueryHandler : IRequestHandler<GetKitchenBoardQuery, KitchenBoardDto>
{
    private static readonly OrderStatus[] BoardStatuses =
        { OrderStatus.Placed, OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Ready };

    private readonly IAppDbContext _db;
    private readonly IBusinessClock _clock;

    public GetKitchenBoardQueryHandler(IAppDbContext db, IBusinessClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<KitchenBoardDto> Handle(GetKitchenBoardQuery request, CancellationToken cancellationToken)
    {
        var date = request.Date ?? _clock.Today;
        var (dayStart, dayEnd) = _clock.DayWindowUtc(date);

        // Station → kitchen routing (small reference sets, loaded once). A line's kitchen is its station's
        // kitchen; station-less lines and stations with no kitchen fall back to the default kitchen.
        var stationKitchen = await _db.KitchenStations
            .Select(s => new { s.Id, s.KitchenId })
            .ToListAsync(cancellationToken);
        var stationToKitchen = stationKitchen.ToDictionary(s => s.Id, s => s.KitchenId);

        var kitchens = await _db.Kitchens
            .Select(k => new { k.Id, k.Name, k.IsDefault })
            .ToListAsync(cancellationToken);
        var kitchenNames = kitchens.ToDictionary(k => k.Id, k => k.Name);
        Guid? defaultKitchenId = kitchens.FirstOrDefault(k => k.IsDefault)?.Id;

        Guid? ResolveKitchen(Guid? stationId)
        {
            if (stationId is { } sid && stationToKitchen.TryGetValue(sid, out var kid) && kid is { } resolved)
                return resolved;
            return defaultKitchenId;
        }

        var query =
            from o in _db.Orders
            join c in _db.Customers on o.CustomerId equals c.Id
            join t in _db.RestaurantTables on o.RestaurantTableId equals t.Id into tj
            from t in tj.DefaultIfEmpty()
            where BoardStatuses.Contains(o.Status)
                  && o.OrderedAtUtc >= dayStart && o.OrderedAtUtc < dayEnd
            select new { Order = o, Customer = c, Table = t };

        if (request.Status is { } st)
            query = query.Where(x => x.Order.Status == st);
        if (request.Type is { } ty)
            query = query.Where(x => x.Order.OrderType == ty);
        if (!string.IsNullOrWhiteSpace(request.TableNumber))
            query = query.Where(x => x.Table != null && x.Table.TableNumber == request.TableNumber);
        if (!string.IsNullOrWhiteSpace(request.SearchOrderNumber))
        {
            var term = request.SearchOrderNumber.Trim().ToUpper();
            query = query.Where(x => x.Order.OrderNumber.Contains(term));
        }
        if (request.StationId is { } stationId)
            query = query.Where(x => x.Order.Lines.Any(l => l.StationId == stationId));
        if (request.KitchenId is { } kitchenFilter)
        {
            // Station ids that resolve to the target kitchen; if the target is the default kitchen,
            // station-less lines (and stations with no kitchen) also belong to it.
            var stationIdsForKitchen = stationToKitchen
                .Where(kv => kv.Value == kitchenFilter || (defaultKitchenId == kitchenFilter && kv.Value == null))
                .Select(kv => kv.Key)
                .ToList();
            var includeStationless = defaultKitchenId == kitchenFilter;

            query = query.Where(x => x.Order.Lines.Any(l =>
                (l.StationId.HasValue && stationIdsForKitchen.Contains(l.StationId.Value))
                || (includeStationless && l.StationId == null)));
        }

        var rows = await query
            .OrderByDescending(x => x.Order.IsPriority)
            .ThenBy(x => x.Order.OrderedAtUtc)
            .Select(x => new
            {
                x.Order.Id,
                x.Order.OrderNumber,
                x.Order.OrderType,
                x.Order.Status,
                TableNumber = x.Table != null ? x.Table.TableNumber : null,
                x.Customer.FullName,
                x.Order.OrderedAtUtc,
                x.Order.PreparingAtUtc,
                x.Order.ReadyAtUtc,
                x.Order.IsPriority,
                x.Order.KitchenNotes,
                CustomerNotes = x.Order.Notes,
                x.Order.WaiterName,
                Lines = x.Order.Lines.Select(l => new
                {
                    l.Quantity,
                    l.Name,
                    l.Notes,
                    l.StationId,
                    l.StationName
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var cards = rows.Select(r =>
        {
            // Resolve each line's kitchen, then narrow to the selected station/kitchen if filtered.
            var lines = r.Lines
                .Select(l =>
                {
                    var kid = ResolveKitchen(l.StationId);
                    return new KitchenItemDto(
                        l.Quantity, l.Name, l.Notes, l.StationId, l.StationName,
                        kid, kid is { } k && kitchenNames.TryGetValue(k, out var name) ? name : null);
                })
                .Where(i => request.StationId is not { } sid || i.StationId == sid)
                .Where(i => request.KitchenId is not { } kf || i.KitchenId == kf)
                .ToList();

            var source = !string.IsNullOrWhiteSpace(r.WaiterName)
                ? $"Waiter · {r.WaiterName}"
                : (r.OrderType == OrderType.DineIn ? "QR / Dine-in" : "Online");

            return new KitchenOrderCardDto(
                r.Id, r.OrderNumber, r.OrderType, r.Status, r.TableNumber,
                string.IsNullOrWhiteSpace(r.FullName) ? null : r.FullName,
                r.OrderedAtUtc, r.PreparingAtUtc, r.ReadyAtUtc,
                r.IsPriority, r.KitchenNotes, r.CustomerNotes, source,
                lines.Sum(i => i.Quantity), lines);
        }).ToList();

        return new KitchenBoardDto(
            cards.Where(c => c.Status is OrderStatus.Placed or OrderStatus.Confirmed).ToList(),
            cards.Where(c => c.Status == OrderStatus.Preparing).ToList(),
            cards.Where(c => c.Status == OrderStatus.Ready).ToList());
    }
}
