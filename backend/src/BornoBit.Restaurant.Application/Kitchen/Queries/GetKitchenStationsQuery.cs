using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Kitchen.Queries;

public record KitchenStationDto(Guid Id, string Name, string? Code, string? ColorHex, int DisplayOrder, bool IsActive);

/// <summary>Active kitchen stations, ordered for the board's station tabs and the product station picker.</summary>
public record GetKitchenStationsQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<KitchenStationDto>>;

public class GetKitchenStationsQueryHandler : IRequestHandler<GetKitchenStationsQuery, IReadOnlyList<KitchenStationDto>>
{
    private readonly IAppDbContext _db;

    public GetKitchenStationsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<KitchenStationDto>> Handle(GetKitchenStationsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.KitchenStations.AsQueryable();
        if (!request.IncludeInactive)
            query = query.Where(s => s.IsActive);

        return await query
            .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name)
            .Select(s => new KitchenStationDto(s.Id, s.Name, s.Code, s.ColorHex, s.DisplayOrder, s.IsActive))
            .ToListAsync(cancellationToken);
    }
}
