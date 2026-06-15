using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Reports;

/// <summary>
/// A Petpooja-style profit &amp; loss over a date range, built straight from the cash-book
/// <see cref="FinanceTransaction"/> rows (single-entry — no GL). Income categories make up revenue;
/// the expense category named <see cref="CogsCategoryName"/> is split out as cost of goods sold so
/// the report can show gross profit, with everything else as operating expense. In this cash-basis
/// book purchases are expensed when paid to the supplier (see RecordSupplierPaymentCommand), so the
/// "Purchases" total is the food/ingredient cost. Date bounds are inclusive, compared by day
/// (mirrors <see cref="GetFinanceSummaryQuery"/>).
/// </summary>
public record GetProfitAndLossQuery(DateTime? FromUtc = null, DateTime? ToUtc = null)
    : IRequest<ProfitAndLossDto>;

public record PlLineDto(Guid CategoryId, string CategoryName, decimal Amount);

public record ProfitAndLossDto(
    decimal TotalRevenue,
    IReadOnlyList<PlLineDto> Revenue,
    decimal TotalCogs,
    IReadOnlyList<PlLineDto> Cogs,
    decimal GrossProfit,
    decimal TotalExpenses,
    IReadOnlyList<PlLineDto> Expenses,
    decimal NetProfit);

public class GetProfitAndLossQueryHandler : IRequestHandler<GetProfitAndLossQuery, ProfitAndLossDto>
{
    /// <summary>Expense category whose total is treated as cost of goods sold — supplier purchases (cash basis).</summary>
    public const string CogsCategoryName = "Purchases";

    private readonly IAppDbContext _db;

    public GetProfitAndLossQueryHandler(IAppDbContext db) => _db = db;

    public async Task<ProfitAndLossDto> Handle(GetProfitAndLossQuery request, CancellationToken cancellationToken)
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
            .Select(g => new { g.Key.CategoryId, g.Key.CategoryName, g.Key.Type, Total = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        var revenue = byCategory
            .Where(c => c.Type == TransactionType.Income)
            .OrderByDescending(c => c.Total)
            .Select(c => new PlLineDto(c.CategoryId, c.CategoryName, c.Total))
            .ToList();

        var cogs = byCategory
            .Where(c => c.Type == TransactionType.Expense
                        && string.Equals(c.CategoryName, CogsCategoryName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.Total)
            .Select(c => new PlLineDto(c.CategoryId, c.CategoryName, c.Total))
            .ToList();

        var expenses = byCategory
            .Where(c => c.Type == TransactionType.Expense
                        && !string.Equals(c.CategoryName, CogsCategoryName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.Total)
            .Select(c => new PlLineDto(c.CategoryId, c.CategoryName, c.Total))
            .ToList();

        var totalRevenue = revenue.Sum(r => r.Amount);
        var totalCogs = cogs.Sum(c => c.Amount);
        var totalExpenses = expenses.Sum(e => e.Amount);
        var grossProfit = totalRevenue - totalCogs;

        return new ProfitAndLossDto(
            totalRevenue, revenue,
            totalCogs, cogs,
            grossProfit,
            totalExpenses, expenses,
            grossProfit - totalExpenses);
    }
}
