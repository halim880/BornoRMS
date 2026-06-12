using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.CashAccounts;

/// <summary>
/// All cash accounts with their running balance = opening balance + Σ income − Σ expense
/// over the (non-deleted) transactions tagged to each account.
/// </summary>
public record GetCashAccountsQuery(bool ActiveOnly = false) : IRequest<IReadOnlyList<CashAccountDto>>;

public class GetCashAccountsQueryHandler : IRequestHandler<GetCashAccountsQuery, IReadOnlyList<CashAccountDto>>
{
    private readonly IAppDbContext _db;

    public GetCashAccountsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CashAccountDto>> Handle(GetCashAccountsQuery request, CancellationToken cancellationToken)
    {
        var accounts = _db.CashAccounts.AsNoTracking();
        if (request.ActiveOnly) accounts = accounts.Where(a => a.IsActive);

        var rows = await accounts
            .OrderBy(a => a.Name)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Kind,
                a.OpeningBalance,
                a.IsActive,
                Income = _db.FinanceTransactions
                    .Where(t => t.CashAccountId == a.Id && t.Type == TransactionType.Income)
                    .Sum(t => (decimal?)t.Amount) ?? 0m,
                Expense = _db.FinanceTransactions
                    .Where(t => t.CashAccountId == a.Id && t.Type == TransactionType.Expense)
                    .Sum(t => (decimal?)t.Amount) ?? 0m
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(a => new CashAccountDto(
                a.Id, a.Name, a.Kind, a.OpeningBalance,
                a.OpeningBalance + a.Income - a.Expense,
                a.IsActive))
            .ToList();
    }
}
