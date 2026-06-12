namespace BornoBit.Restaurant.Domain.Accounting;

/// <summary>The nature of a cash account — where the money physically sits.</summary>
public enum CashAccountKind
{
    Cash = 0,
    MobileWallet = 1,
    Bank = 2
}
