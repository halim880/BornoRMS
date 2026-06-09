using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Dining.Queries;

public record GetTablesQuery() : IRequest<IReadOnlyList<TableDto>>;

public record TableDto(Guid Id, string TableNumber, int Capacity);

public class GetTablesQueryHandler : IRequestHandler<GetTablesQuery, IReadOnlyList<TableDto>>
{
    private readonly IAppDbContext _db;

    public GetTablesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<TableDto>> Handle(GetTablesQuery request, CancellationToken cancellationToken)
    {
        return await _db.RestaurantTables
            .Where(t => t.IsActive)
            .OrderBy(t => t.TableNumber)
            .Select(t => new TableDto(t.Id, t.TableNumber, t.Capacity))
            .ToListAsync(cancellationToken);
    }
}
