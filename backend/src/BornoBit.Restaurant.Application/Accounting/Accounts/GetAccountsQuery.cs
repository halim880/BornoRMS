using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Accounts;

/// <summary>Flat list of accounts, ordered by code. Set <paramref name="PostableOnly"/> for line pickers.</summary>
public record GetAccountsQuery(bool PostableOnly = false, bool ActiveOnly = false)
    : IRequest<IReadOnlyList<AccountDto>>;

public class GetAccountsQueryHandler : IRequestHandler<GetAccountsQuery, IReadOnlyList<AccountDto>>
{
    private readonly IAppDbContext _db;

    public GetAccountsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AccountDto>> Handle(GetAccountsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Accounts.AsNoTracking();

        if (request.PostableOnly) query = query.Where(a => a.IsPostable);
        if (request.ActiveOnly) query = query.Where(a => a.IsActive);

        // NormalBalance is a computed (EF-ignored) property, so project the columns we
        // can translate and build the DTO in memory.
        var rows = await query
            .OrderBy(a => a.Code)
            .Select(a => new
            {
                a.Id, a.Code, a.Name, a.AccountType,
                a.ParentId, a.IsPostable, a.IsActive, a.Description
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(a => new AccountDto(
                a.Id, a.Code, a.Name, a.AccountType, NormalBalanceOf(a.AccountType),
                a.ParentId, a.IsPostable, a.IsActive, a.Description))
            .ToList();
    }

    internal static NormalBalance NormalBalanceOf(AccountType type) =>
        type is AccountType.Asset or AccountType.Expense ? NormalBalance.Debit : NormalBalance.Credit;
}
