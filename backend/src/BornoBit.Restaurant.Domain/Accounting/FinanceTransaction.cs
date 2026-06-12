using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Accounting;

/// <summary>
/// A single income or expense entry: an amount moving in or out of a cash account on a
/// given day, filed under a category. Single-entry — no balancing counterpart.
/// </summary>
public class FinanceTransaction : AuditableEntity
{
    public string Number { get; private set; } = default!;
    public DateTime OccurredOn { get; private set; }
    public TransactionType Type { get; private set; }
    public Guid CashAccountId { get; private set; }
    public Guid CategoryId { get; private set; }
    public decimal Amount { get; private set; }
    public string? Reference { get; private set; }
    public string? Notes { get; private set; }

    private FinanceTransaction() { }

    public static FinanceTransaction Create(
        string number,
        DateTime occurredOn,
        TransactionType type,
        Guid cashAccountId,
        Guid categoryId,
        decimal amount,
        string? reference = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(number)) throw new ArgumentException("Number is required.", nameof(number));
        if (cashAccountId == Guid.Empty) throw new ArgumentException("Cash account is required.", nameof(cashAccountId));
        if (categoryId == Guid.Empty) throw new ArgumentException("Category is required.", nameof(categoryId));
        if (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        return new FinanceTransaction
        {
            Number = number.Trim().ToUpperInvariant(),
            OccurredOn = occurredOn.Date,
            Type = type,
            CashAccountId = cashAccountId,
            CategoryId = categoryId,
            Amount = amount,
            Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
    }

    public void Update(
        DateTime occurredOn,
        TransactionType type,
        Guid cashAccountId,
        Guid categoryId,
        decimal amount,
        string? reference,
        string? notes)
    {
        if (cashAccountId == Guid.Empty) throw new ArgumentException("Cash account is required.", nameof(cashAccountId));
        if (categoryId == Guid.Empty) throw new ArgumentException("Category is required.", nameof(categoryId));
        if (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        OccurredOn = occurredOn.Date;
        Type = type;
        CashAccountId = cashAccountId;
        CategoryId = categoryId;
        Amount = amount;
        Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }
}
