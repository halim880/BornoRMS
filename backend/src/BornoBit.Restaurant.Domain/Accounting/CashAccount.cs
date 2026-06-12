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

    private CashAccount() { }

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
