using BornoBit.Restaurant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Accounts;

/// <summary>The Chart of Accounts as a tree (roots → children), ordered by code at each level.</summary>
public record GetChartOfAccountsTreeQuery(bool ActiveOnly = false)
    : IRequest<IReadOnlyList<AccountNodeDto>>;

public class GetChartOfAccountsTreeQueryHandler
    : IRequestHandler<GetChartOfAccountsTreeQuery, IReadOnlyList<AccountNodeDto>>
{
    private readonly IAppDbContext _db;

    public GetChartOfAccountsTreeQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AccountNodeDto>> Handle(GetChartOfAccountsTreeQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Accounts.AsNoTracking();
        if (request.ActiveOnly) query = query.Where(a => a.IsActive);

        var flat = await query
            .OrderBy(a => a.Code)
            .Select(a => new { a.Id, a.Code, a.Name, a.AccountType, a.ParentId, a.IsPostable, a.IsActive })
            .ToListAsync(cancellationToken);

        var byParent = flat.ToLookup(a => a.ParentId);

        IReadOnlyList<AccountNodeDto> Build(Guid? parentId) =>
            byParent[parentId]
                .Select(a => new AccountNodeDto(
                    a.Id, a.Code, a.Name, a.AccountType,
                    GetAccountsQueryHandler.NormalBalanceOf(a.AccountType),
                    a.IsPostable, a.IsActive, Build(a.Id)))
                .ToList();

        return Build(null);
    }
}
