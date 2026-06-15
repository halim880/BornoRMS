using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Accounting;

/// <summary>
/// One leg of a journal entry: a debit or a credit against a single postable account.
/// Exactly one of <see cref="Debit"/> / <see cref="Credit"/> is positive; the other is zero.
/// </summary>
public class JournalLine : BaseEntity
{
    public Guid JournalEntryId { get; set; }
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? LineNarration { get; set; }
}
