using BornoBit.Restaurant.Domain.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds a small restaurant's starting books: a few cash accounts (drawer, mobile wallet, bank)
/// and the usual income/expense categories. Idempotent — skips entirely once any cash account exists.
/// </summary>
public class AccountingSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AccountingSeeder> _logger;

    public AccountingSeeder(ApplicationDbContext db, ILogger<AccountingSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    private static readonly (string Name, CashAccountKind Kind)[] CashAccounts =
    {
        ("Cash in Hand", CashAccountKind.Cash),
        ("bKash", CashAccountKind.MobileWallet),
        ("Bank", CashAccountKind.Bank),
    };

    private static readonly (string Name, TransactionType Type)[] Categories =
    {
        ("Sales", TransactionType.Income),
        ("Other Income", TransactionType.Income),
        ("Refunds", TransactionType.Expense),
        ("Purchases", TransactionType.Expense),
        ("Salaries", TransactionType.Expense),
        ("Rent", TransactionType.Expense),
        ("Utilities", TransactionType.Expense),
        ("Miscellaneous", TransactionType.Expense),
    };

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.CashAccounts.AnyAsync(cancellationToken)) return;

        foreach (var (name, kind) in CashAccounts)
            _db.CashAccounts.Add(CashAccount.Create(name, kind));

        foreach (var (name, type) in Categories)
            _db.FinanceCategories.Add(FinanceCategory.Create(name, type));

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "AccountingSeeder: seeded {Accounts} cash accounts and {Categories} categories.",
            CashAccounts.Length, Categories.Length);
    }
}
