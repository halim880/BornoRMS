using BornoBit.Restaurant.Application.Accounting.Posting;
using BornoBit.Restaurant.Domain.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Seeding;

/// <summary>
/// Builds the double-entry general ledger's Chart of Accounts — a full, deep restaurant chart — on top of
/// the single-entry cash book. Idempotent and NON-DESTRUCTIVE: the template is upserted by code every boot
/// (create if missing, otherwise re-file name/parent/type), existing cash-account leaves are re-homed under
/// their kind group, and nothing is ever deleted (journal lines reference accounts). Cash accounts and
/// finance categories are mapped to postable leaves via <see cref="ChartOfAccountsMapper"/>; an
/// opening-balance journal is posted once. Runs after <see cref="AccountingSeeder"/>.
/// </summary>
public class GeneralLedgerSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<GeneralLedgerSeeder> _logger;

    public GeneralLedgerSeeder(ApplicationDbContext db, ILogger<GeneralLedgerSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public const string OpeningEquityCode = "3100";

    private static readonly AccountType As = AccountType.Asset;
    private static readonly AccountType Li = AccountType.Liability;
    private static readonly AccountType Eq = AccountType.Equity;
    private static readonly AccountType In = AccountType.Income;
    private static readonly AccountType Ex = AccountType.Expense;

    // (Code, Name, Type, ParentCode, IsPostable). Parents appear before their children.
    // Existing dynamic leaves (cash accounts 10xx, category accounts 40xx/50xx) are intentionally NOT
    // listed — they are kept and re-filed, not replaced.
    private static readonly (string Code, string Name, AccountType Type, string? Parent, bool Postable)[] Template =
    {
        // ----- Assets -----
        ("1000", "Assets", As, null, false),
        ("1100", "Current Assets", As, "1000", false),
        ("1110", "Cash", As, "1100", false),
        ("1120", "Mobile Financial Services", As, "1100", false),
        ("1130", "Bank Accounts", As, "1100", false),
        ("1140", "Accounts Receivable", As, "1100", true),
        ("1150", "Advance Payments", As, "1100", true),
        ("1160", "Payment Clearing Accounts", As, "1100", false),
        ("1161", "Bkash Settlement Pending", As, "1160", true),
        ("1162", "Nagad Settlement Pending", As, "1160", true),
        ("1163", "Card Settlement Pending", As, "1160", true),
        ("1164", "FoodPanda Settlement Pending", As, "1160", true),
        ("1300", "Fixed Assets", As, "1000", false),
        ("1310", "Furniture & Fixtures", As, "1300", true),
        ("1320", "Kitchen Equipment", As, "1300", true),
        ("1330", "POS Equipment", As, "1300", true),
        ("1340", "Computers", As, "1300", true),
        ("1350", "Vehicles", As, "1300", true),

        // ----- Liabilities -----
        ("2000", "Liabilities", Li, null, false),
        ("2100", "Accounts Payable", Li, "2000", true),
        ("2200", "VAT Payable", Li, "2000", true),
        ("2300", "Employee Payable", Li, "2000", true),
        ("2400", "Tax Payable", Li, "2000", true),
        ("2500", "Loan Payable", Li, "2000", true),
        ("2600", "Customer Advance", Li, "2000", true),

        // ----- Equity -----
        ("3000", "Equity", Eq, null, false),
        ("3100", "Opening Balance Equity", Eq, "3000", true),
        ("3200", "Retained Earnings", Eq, "3000", true),
        ("3300", "Owner Capital", Eq, "3000", true),
        ("3400", "Partner Capital", Eq, "3000", true),
        ("3500", "Current Year Earnings", Eq, "3000", true),

        // ----- Revenue (Income) -----
        ("4000", "Revenue", In, null, false),
        ("4030", "Food Sales", In, "4000", true),
        ("4040", "Beverage Sales", In, "4000", true),
        ("4050", "Delivery Charge Income", In, "4000", true),
        ("4060", "Service Charge Income", In, "4000", true),
        ("4070", "Catering Income", In, "4000", true),
        ("4080", "Discount Recovery", In, "4000", true),

        // ----- Operating Expenses -----
        ("5000", "Expenses", Ex, null, false),
        ("5110", "Salary Expense", Ex, "5000", true),
        ("5120", "Overtime Expense", Ex, "5000", true),
        ("5130", "Rent Expense", Ex, "5000", true),
        ("5140", "Electricity Expense", Ex, "5000", true),
        ("5150", "Gas Expense", Ex, "5000", true),
        ("5160", "Water Expense", Ex, "5000", true),
        ("5170", "Internet Expense", Ex, "5000", true),
        ("5180", "Cleaning Expense", Ex, "5000", true),
        ("5190", "Maintenance Expense", Ex, "5000", true),
        ("5200", "Marketing Expense", Ex, "5000", true),
        ("5210", "Merchant Charge Expense", Ex, "5000", true),
        ("5220", "Bank Charge Expense", Ex, "5000", true),
        ("5230", "Software Expense", Ex, "5000", true),
        ("5240", "Delivery Expense", Ex, "5000", true),
        ("5250", "Depreciation Expense", Ex, "5000", true),

        // ----- Cost of Goods Sold -----
        ("6000", "Cost of Goods Sold", Ex, null, false),
        ("6010", "Food Cost", Ex, "6000", true),
        ("6020", "Beverage Cost", Ex, "6000", true),
        ("6030", "Packaging Cost", Ex, "6000", true),
        ("6040", "Inventory Adjustment", Ex, "6000", true),
        ("6050", "Wastage Cost", Ex, "6000", true),

        // ----- Fallback -----
        ("9000", "Suspense", As, "1000", true),
    };

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.Database.GetPendingMigrationsAsync(cancellationToken) is { } pending && pending.Any())
        {
            _logger.LogWarning("GeneralLedgerSeeder: pending migrations; skipping.");
            return;
        }

        await UpsertTemplateAsync(cancellationToken);
        await RehomeCashLeavesAsync(cancellationToken);
        await MapCashAccountsAsync(cancellationToken);
        await MapCategoriesAsync(cancellationToken);
        await SeedOpeningBalancesAsync(cancellationToken);
    }

    // Create-or-update each template account by code, resolving parents from earlier rows.
    private async Task UpsertTemplateAsync(CancellationToken cancellationToken)
    {
        var existing = await _db.Accounts.IgnoreQueryFilters().ToListAsync(cancellationToken);
        var byCode = existing.ToDictionary(a => a.Code, StringComparer.OrdinalIgnoreCase);

        var changed = false;
        foreach (var (code, name, type, parentCode, postable) in Template)
        {
            Guid? parentId = parentCode is null ? null
                : byCode.TryGetValue(parentCode, out var parent) ? parent.Id : null;

            if (byCode.TryGetValue(code, out var acc))
            {
                // Re-file existing account to match the template (name/type/parent/postable), keep description.
                if (acc.Name != name || acc.AccountType != type || acc.ParentId != parentId || acc.IsPostable != postable)
                {
                    acc.Update(name, type, parentId, postable, acc.Description);
                    changed = true;
                }
            }
            else
            {
                acc = Account.Create(code, name, type, parentId, postable);
                _db.Accounts.Add(acc);
                byCode[code] = acc;
                changed = true;
            }
        }

        if (changed)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("GeneralLedgerSeeder: chart of accounts upserted ({Count} template rows).", Template.Length);
        }
    }

    // Re-home each mapped cash-account leaf under its kind group (Cash / MFS / Bank). Idempotent.
    private async Task RehomeCashLeavesAsync(CancellationToken cancellationToken)
    {
        var groups = await _db.Accounts
            .Where(a => a.Code == ChartOfAccountsMapper.CashGroupCode
                     || a.Code == ChartOfAccountsMapper.MfsGroupCode
                     || a.Code == ChartOfAccountsMapper.BankGroupCode)
            .ToDictionaryAsync(a => a.Code, a => a.Id, cancellationToken);
        if (groups.Count < 3) return;

        var cashAccounts = await _db.CashAccounts.Where(a => a.GlAccountId != null).ToListAsync(cancellationToken);
        var leafIds = cashAccounts.Select(a => a.GlAccountId!.Value).ToList();
        var leaves = (await _db.Accounts.Where(a => leafIds.Contains(a.Id)).ToListAsync(cancellationToken))
            .ToDictionary(a => a.Id);

        var changed = false;
        foreach (var ca in cashAccounts)
        {
            if (!leaves.TryGetValue(ca.GlAccountId!.Value, out var leaf)) continue;
            var target = groups[ChartOfAccountsMapper.GroupCodeFor(ca.Kind)];
            if (leaf.ParentId != target)
            {
                leaf.Update(leaf.Name, leaf.AccountType, target, leaf.IsPostable, leaf.Description);
                changed = true;
            }
        }
        if (changed) await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task MapCashAccountsAsync(CancellationToken cancellationToken)
    {
        var unmapped = await _db.CashAccounts.Where(a => a.GlAccountId == null).OrderBy(a => a.Name).ToListAsync(cancellationToken);
        foreach (var ca in unmapped)
        {
            await ChartOfAccountsMapper.EnsureCashAccountGlAsync(_db, ca, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken); // persist each leaf so the next code allocation sees it
        }
    }

    private async Task MapCategoriesAsync(CancellationToken cancellationToken)
    {
        var unmapped = await _db.FinanceCategories.Where(c => c.GlAccountId == null).OrderBy(c => c.Type).ThenBy(c => c.Name).ToListAsync(cancellationToken);
        foreach (var cat in unmapped)
        {
            await ChartOfAccountsMapper.EnsureCategoryGlAsync(_db, cat, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    // One opening journal (once): Dr each cash GL asset = opening balance, Cr Opening Balance Equity.
    private async Task SeedOpeningBalancesAsync(CancellationToken cancellationToken)
    {
        if (await _db.JournalEntries.IgnoreQueryFilters().AnyAsync(e => e.Reference == "OPENING", cancellationToken))
            return;

        var cashAccounts = await _db.CashAccounts
            .Where(a => a.GlAccountId != null && a.OpeningBalance != 0m)
            .ToListAsync(cancellationToken);
        if (cashAccounts.Count == 0) return;

        var equityId = await _db.Accounts.Where(a => a.Code == OpeningEquityCode).Select(a => a.Id).FirstAsync(cancellationToken);
        var total = cashAccounts.Sum(a => a.OpeningBalance);

        var entry = JournalEntry.Create("JV-OPENING-0001", DateTime.UtcNow.Date, VoucherType.Journal,
            reference: "OPENING", narration: "Opening balances");
        foreach (var ca in cashAccounts)
            entry.AddLine(ca.GlAccountId!.Value, debit: ca.OpeningBalance, credit: 0m, lineNarration: $"Opening {ca.Name}");
        entry.AddLine(equityId, debit: 0m, credit: total, lineNarration: "Opening balance equity");
        entry.Post(DateTime.UtcNow);

        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("GeneralLedgerSeeder: posted opening balances ({Total:0.00}).", total);
    }
}
