namespace BornoBit.Restaurant.Domain.Accounting;

/// <summary>
/// Lifecycle of a journal entry. Only <c>Posted</c> entries affect ledger balances;
/// posted entries are never edited or deleted — they are reversed via <c>Void</c>.
/// </summary>
public enum JournalStatus
{
    Draft = 1,
    Posted = 2,
    Void = 3
}
