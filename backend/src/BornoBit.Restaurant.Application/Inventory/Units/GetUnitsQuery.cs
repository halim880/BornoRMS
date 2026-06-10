using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Units;

/// <summary>Units of measure for stock entry (seed-driven; includes BD units mon/seer/hali).</summary>
public record GetUnitsQuery : IRequest<IReadOnlyList<UnitDto>>;

public class GetUnitsQueryHandler : IRequestHandler<GetUnitsQuery, IReadOnlyList<UnitDto>>
{
    private readonly IAppDbContext _db;

    public GetUnitsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<UnitDto>> Handle(GetUnitsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Units
            .OrderBy(u => u.Dimension).ThenBy(u => u.ToBaseFactor)
            .Select(u => new UnitDto(u.Id, u.Code, u.Name, u.BanglaName, u.Dimension, u.ToBaseFactor, u.IsActive))
            .ToListAsync(cancellationToken);
    }
}
