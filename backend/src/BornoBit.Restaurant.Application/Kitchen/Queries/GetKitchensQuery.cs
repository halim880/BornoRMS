using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Kitchen.Queries;

public record KitchenDto(Guid Id, string Name, string? Code, string? ColorHex, string? PrinterName, int DisplayOrder, bool IsDefault, bool IsActive);

/// <summary>Active kitchens, ordered for the board's kitchen selector and the station kitchen picker.</summary>
public record GetKitchensQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<KitchenDto>>;

public class GetKitchensQueryHandler : IRequestHandler<GetKitchensQuery, IReadOnlyList<KitchenDto>>
{
    private readonly IAppDbContext _db;

    public GetKitchensQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<KitchenDto>> Handle(GetKitchensQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Kitchens.AsQueryable();
        if (!request.IncludeInactive)
            query = query.Where(k => k.IsActive);

        return await query
            .OrderBy(k => k.DisplayOrder).ThenBy(k => k.Name)
            .Select(k => new KitchenDto(k.Id, k.Name, k.Code, k.ColorHex, k.PrinterName, k.DisplayOrder, k.IsDefault, k.IsActive))
            .ToListAsync(cancellationToken);
    }
}
