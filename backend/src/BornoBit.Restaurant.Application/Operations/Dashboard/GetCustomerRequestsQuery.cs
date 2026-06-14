using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Dining;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Dashboard;

/// <summary>Section-6 customer requests. Defaults to the pending queue; pass a status to widen it.</summary>
public record GetCustomerRequestsQuery(CustomerRequestStatus? Status = CustomerRequestStatus.Pending)
    : IRequest<IReadOnlyList<CustomerRequestRowDto>>;

public record CustomerRequestRowDto(
    Guid Id,
    Guid RestaurantTableId,
    string TableNumber,
    CustomerRequestType Type,
    CustomerRequestStatus Status,
    DateTime RequestedAtUtc,
    int WaitingMinutes,
    string? Note);

public class GetCustomerRequestsQueryHandler : IRequestHandler<GetCustomerRequestsQuery, IReadOnlyList<CustomerRequestRowDto>>
{
    private readonly IAppDbContext _db;

    public GetCustomerRequestsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CustomerRequestRowDto>> Handle(GetCustomerRequestsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var query =
            from r in _db.CustomerRequests
            join t in _db.RestaurantTables on r.RestaurantTableId equals t.Id
            select new { Request = r, t.TableNumber };

        if (request.Status is { } st)
            query = query.Where(x => x.Request.Status == st);

        var rows = await query
            .OrderBy(x => x.Request.Status)
            .ThenBy(x => x.Request.RequestedAtUtc)
            .Select(x => new
            {
                x.Request.Id,
                x.Request.RestaurantTableId,
                x.TableNumber,
                x.Request.Type,
                x.Request.Status,
                x.Request.RequestedAtUtc,
                x.Request.Note
            })
            .ToListAsync(cancellationToken);

        return rows.Select(x => new CustomerRequestRowDto(
            x.Id, x.RestaurantTableId, x.TableNumber, x.Type, x.Status, x.RequestedAtUtc,
            (int)Math.Max(0, (now - x.RequestedAtUtc).TotalMinutes), x.Note)).ToList();
    }
}
