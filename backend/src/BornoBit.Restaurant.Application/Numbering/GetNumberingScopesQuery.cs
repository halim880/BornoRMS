using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Numbering;

public record GetNumberingScopesQuery(bool IncludeInactive = true) : IRequest<IReadOnlyList<NumberingScopeDto>>;

public class GetNumberingScopesQueryHandler : IRequestHandler<GetNumberingScopesQuery, IReadOnlyList<NumberingScopeDto>>
{
    private readonly IAppDbContext _db;

    public GetNumberingScopesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<NumberingScopeDto>> Handle(GetNumberingScopesQuery request, CancellationToken cancellationToken)
    {
        var q = _db.NumberingScopes.AsQueryable();
        if (!request.IncludeInactive) q = q.Where(s => s.IsActive);

        return await q
            .OrderBy(s => s.Code)
            .Select(s => new NumberingScopeDto(
                s.Id, s.Code, s.Name, s.Prefix, s.Cadence, s.Digits, s.ResetByOutlet, s.IsActive, s.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
