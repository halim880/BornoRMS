using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Transactions;

/// <summary>Filterable, paged list of finance transactions, newest first.</summary>
public record GetTransactionsQuery(
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    TransactionType? Type = null,
    Guid? CategoryId = null,
    Guid? CashAccountId = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<TransactionListItemDto>>;

public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, PagedResult<TransactionListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetTransactionsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<TransactionListItemDto>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query =
            from t in _db.FinanceTransactions
            join c in _db.FinanceCategories on t.CategoryId equals c.Id
            join a in _db.CashAccounts on t.CashAccountId equals a.Id
            select new { Txn = t, Category = c, Account = a };

        if (request.FromUtc is { } from)
            query = query.Where(x => x.Txn.OccurredOn >= from.Date);
        if (request.ToUtc is { } to)
            query = query.Where(x => x.Txn.OccurredOn <= to.Date);
        if (request.Type is { } type)
            query = query.Where(x => x.Txn.Type == type);
        if (request.CategoryId is { } cat)
            query = query.Where(x => x.Txn.CategoryId == cat);
        if (request.CashAccountId is { } acc)
            query = query.Where(x => x.Txn.CashAccountId == acc);

        var total = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Txn.OccurredOn).ThenByDescending(x => x.Txn.Number)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TransactionListItemDto(
                x.Txn.Id,
                x.Txn.Number,
                x.Txn.OccurredOn,
                x.Txn.Type,
                x.Txn.CategoryId,
                x.Category.Name,
                x.Txn.CashAccountId,
                x.Account.Name,
                x.Txn.Amount,
                x.Txn.Reference,
                x.Txn.Notes))
            .ToListAsync(cancellationToken);

        return new PagedResult<TransactionListItemDto>(items, page, pageSize, total);
    }
}
