namespace BornoBit.Restaurant.Domain.Ordering;

/// <summary>
/// Mobile-banking provider for a <see cref="PaymentMethod.Mobile"/> payment. Null/None for cash and card.
/// </summary>
public enum PaymentProvider
{
    None = 0,
    Bkash = 1,
    Nagad = 2,
    Rocket = 3,
    Upay = 4,
    Other = 99
}
