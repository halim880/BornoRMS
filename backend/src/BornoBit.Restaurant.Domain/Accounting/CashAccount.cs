using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Accounting;

/// <summary>
/// A place money sits — Cash drawer, a mobile wallet (bKash), or a bank account.
/// Carries an opening balance; the running balance is opening + incomes − expenses
/// over the transactions tagged to it (computed in the query layer, not stored).
/// </summary>
public class CashAccount : AuditableEntity
{
    public string Name { get; private set; } = default!;
    public CashAccountKind Kind { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>The GL asset account this cash account posts to (set by the GL seeder / auto-create). Null = unmapped.</summary>
    public Guid? GlAccountId { get; private set; }

    private CashAccount() { }

    /// <summary>Links this cash account to its GL asset account (idempotent map step).</summary>
    public void MapToGlAccount(Guid glAccountId) => GlAccountId = glAccountId;

    public static CashAccount Create(string name, CashAccountKind kind, decimal openingBalance = 0m)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));

        return new CashAccount
        {
            Name = name.Trim(),
            Kind = kind,
            OpeningBalance = openingBalance,
            IsActive = true
        };
    }

    public void Update(string name, CashAccountKind kind, decimal openingBalance)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        Name = name.Trim();
        Kind = kind;
        OpeningBalance = openingBalance;
    }

    public void SetActive(bool active) => IsActive = active;
}
