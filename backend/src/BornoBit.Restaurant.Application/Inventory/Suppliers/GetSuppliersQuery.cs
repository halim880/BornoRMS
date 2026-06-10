using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Suppliers;

public record GetSuppliersQuery : IRequest<IReadOnlyList<SupplierDto>>;

public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, IReadOnlyList<SupplierDto>>
{
    private readonly IAppDbContext _db;

    public GetSuppliersQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<SupplierDto>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        return await _db.Suppliers
            .OrderBy(s => s.Name)
            .Select(s => new SupplierDto(s.Id, s.Code, s.Name, s.Phone, s.Address, s.PaymentTermsDays, s.Notes, s.IsActive))
            .ToListAsync(cancellationToken);
    }
}
