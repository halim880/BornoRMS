using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Reports;

/// <summary>
/// Period totals for the Transactions summary cards: total income, total expense, net, and a
/// per-category breakdown. The optional date bounds are inclusive (compared by day).
/// </summary>
public record GetFinanceSummaryQuery(DateTime? FromUtc = null, DateTime? ToUtc = null)
    : IRequest<FinanceSummaryDto>;

public record CategoryTotalDto(Guid CategoryId, string CategoryName, TransactionType Type, decimal Total);

public record FinanceSummaryDto(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Net,
    IReadOnlyList<CategoryTotalDto> ByCategory);

public class GetFinanceSummaryQueryHandler : IRequestHandler<GetFinanceSummaryQuery, FinanceSummaryDto>
{
    private readonly IAppDbContext _db;

    public GetFinanceSummaryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<FinanceSummaryDto> Handle(GetFinanceSummaryQuery request, CancellationToken cancellationToken)
    {
        var query =
            from t in _db.FinanceTransactions
            join c in _db.FinanceCategories on t.CategoryId equals c.Id
            select new { t.CategoryId, CategoryName = c.Name, t.Type, t.Amount, t.OccurredOn };

        if (request.FromUtc is { } from)
            query = query.Where(x => x.OccurredOn >= from.Date);
        if (request.ToUtc is { } to)
            query = query.Where(x => x.OccurredOn <= to.Date);

        var byCategory = await query
            .GroupBy(x => new { x.CategoryId, x.CategoryName, x.Type })
            .Select(g => new CategoryTotalDto(g.Key.CategoryId, g.Key.CategoryName, g.Key.Type, g.Sum(x => x.Amount)))
            .ToListAsync(cancellationToken);

        var totalIncome = byCategory.Where(c => c.Type == TransactionType.Income).Sum(c => c.Total);
        var totalExpense = byCategory.Where(c => c.Type == TransactionType.Expense).Sum(c => c.Total);

        var ordered = byCategory
            .OrderBy(c => c.Type).ThenByDescending(c => c.Total)
            .ToList();

        return new FinanceSummaryDto(totalIncome, totalExpense, totalIncome - totalExpense, ordered);
    }
}
