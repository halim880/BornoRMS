using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Transactions;

/// <summary>
/// Shared invariants for creating/updating a transaction: the category must exist and match the
/// transaction's <see cref="TransactionType"/>, and the cash account must exist and be active.
/// </summary>
internal static class TransactionGuards
{
    public static async Task EnsureValidAsync(
        IAppDbContext db, TransactionType type, Guid categoryId, Guid cashAccountId, CancellationToken cancellationToken)
    {
        var category = await db.FinanceCategories
            .Select(c => new { c.Id, c.Type })
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken)
            ?? throw new NotFoundException("Category not found.");
        if (category.Type != type)
            throw new ConflictException($"The selected category is an {category.Type} category, not {type}.");

        var account = await db.CashAccounts
            .Select(a => new { a.Id, a.IsActive })
            .FirstOrDefaultAsync(a => a.Id == cashAccountId, cancellationToken)
            ?? throw new NotFoundException("Cash account not found.");
        if (!account.IsActive)
            throw new ConflictException("The selected cash account is inactive.");
    }
}
