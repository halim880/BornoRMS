using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Dining.Queries;

/// <summary>
/// Returns every table (active and inactive) for the admin page.
/// The customer/waiter flows use <see cref="GetTablesQuery"/>, which filters to active only.
/// </summary>
public record GetAllTablesQuery() : IRequest<IReadOnlyList<TableAdminDto>>;

public record TableAdminDto(Guid Id, string TableNumber, int Capacity, bool IsActive);

public class GetAllTablesQueryHandler : IRequestHandler<GetAllTablesQuery, IReadOnlyList<TableAdminDto>>
{
    private readonly IAppDbContext _db;

    public GetAllTablesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<TableAdminDto>> Handle(GetAllTablesQuery request, CancellationToken cancellationToken)
    {
        return await _db.RestaurantTables
            .OrderBy(t => t.TableNumber)
            .Select(t => new TableAdminDto(t.Id, t.TableNumber, t.Capacity, t.IsActive))
            .ToListAsync(cancellationToken);
    }
}
