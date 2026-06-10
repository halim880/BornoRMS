using BornoBit.Restaurant.Domain.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds a standard single-restaurant Chart of Accounts: five top-level group accounts
/// (one per <see cref="AccountType"/>) with postable leaf accounts beneath. Idempotent —
/// skips entirely once any account exists.
/// </summary>
public class ChartOfAccountsSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ChartOfAccountsSeeder> _logger;

    public ChartOfAccountsSeeder(ApplicationDbContext db, ILogger<ChartOfAccountsSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    private sealed record Group(string Code, string Name, AccountType Type, (string Code, string Name)[] Leaves);

    private static readonly Group[] Defaults =
    {
        new("1000", "Assets", AccountType.Asset, new[]
        {
            ("1010", "Cash in Hand"),
            ("1020", "bKash"),
            ("1030", "Bank"),
            ("1040", "Inventory"),
        }),
        new("2000", "Liabilities", AccountType.Liability, new[]
        {
            ("2010", "Accounts Payable"),
            ("2020", "VAT Payable"),
        }),
        new("3000", "Equity", AccountType.Equity, new[]
        {
            ("3010", "Owner's Capital"),
            ("3020", "Retained Earnings"),
        }),
        new("4000", "Income", AccountType.Income, new[]
        {
            ("4010", "Food Sales"),
            ("4020", "Other Income"),
        }),
        new("5000", "Expenses", AccountType.Expense, new[]
        {
            ("5010", "Food Purchases"),
            ("5020", "Wastage"),
            ("5030", "Salaries"),
            ("5040", "Rent"),
            ("5050", "Utilities"),
            ("5060", "Miscellaneous"),
        }),
    };

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.Accounts.AnyAsync(cancellationToken)) return;

        var leaves = 0;
        foreach (var group in Defaults)
        {
            var root = Account.Create(group.Code, group.Name, group.Type, parentId: null, isPostable: false);
            _db.Accounts.Add(root);
            await _db.SaveChangesAsync(cancellationToken); // need root.Id for the children

            foreach (var (code, name) in group.Leaves)
            {
                _db.Accounts.Add(Account.Create(code, name, group.Type, parentId: root.Id, isPostable: true));
                leaves++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("ChartOfAccountsSeeder: seeded {Groups} groups and {Leaves} accounts.", Defaults.Length, leaves);
    }
}
