using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Categories;

/// <summary>Categories, optionally filtered by type and/or active-only, ordered by type then name.</summary>
public record GetCategoriesQuery(TransactionType? Type = null, bool ActiveOnly = false)
    : IRequest<IReadOnlyList<CategoryDto>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    private readonly IAppDbContext _db;

    public GetCategoriesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.FinanceCategories.AsNoTracking();

        if (request.Type is { } type) query = query.Where(c => c.Type == type);
        if (request.ActiveOnly) query = query.Where(c => c.IsActive);

        return await query
            .OrderBy(c => c.Type).ThenBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Type, c.IsActive))
            .ToListAsync(cancellationToken);
    }
}
