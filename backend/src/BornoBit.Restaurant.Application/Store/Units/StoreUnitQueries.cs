using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Store;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Store.Units;

public record StoreUnitDto(
    Guid Id,
    string Code,
    string Name,
    string? BanglaName,
    StoreUnitDimension Dimension,
    decimal ToBaseFactor,
    bool IsActive);

/// <summary>Store units of measure for stock entry (seed-driven; includes BD units mon/seer/bosta).</summary>
public record GetStoreUnitsQuery : IRequest<IReadOnlyList<StoreUnitDto>>;

public class GetStoreUnitsQueryHandler : IRequestHandler<GetStoreUnitsQuery, IReadOnlyList<StoreUnitDto>>
{
    private readonly IAppDbContext _db;

    public GetStoreUnitsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<StoreUnitDto>> Handle(GetStoreUnitsQuery request, CancellationToken cancellationToken)
    {
        return await _db.StoreUnits
            .OrderBy(u => u.Dimension).ThenBy(u => u.ToBaseFactor)
            .Select(u => new StoreUnitDto(u.Id, u.Code, u.Name, u.BanglaName, u.Dimension, u.ToBaseFactor, u.IsActive))
            .ToListAsync(cancellationToken);
    }
}
