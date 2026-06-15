namespace BornoBit.Restaurant.Domain.Accounting;

/// <summary>
/// The five fundamental account classifications. Determines a postable account's
/// <see cref="Account.NormalBalance"/> and where it lands on the financial statements
/// (Asset/Liability/Equity → Balance Sheet; Income/Expense → Profit &amp; Loss).
/// </summary>
public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Income = 4,
    Expense = 5
}
