using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Accounting;

/// <summary>
/// A balanced double-entry transaction (aggregate root). Created as <c>Draft</c>, then
/// <c>Post</c> freezes it — posting is the only path that affects ledger balances and
/// enforces the core invariant: total debits == total credits, total &gt; 0, at least two
/// lines. Posted entries are immutable; reverse via <see cref="Void"/>, never delete.
/// Mirrors the GoodsReceipt Draft→Post model.
/// </summary>
public class JournalEntry : AuditableEntity
{
    public string EntryNumber { get; private set; } = default!;
    public DateTime EntryDate { get; private set; }
    public VoucherType VoucherType { get; private set; }
    public string? Reference { get; private set; }
    public string? Narration { get; private set; }
    public string Currency { get; private set; } = "Tk";
    public JournalStatus Status { get; private set; } = JournalStatus.Draft;
    public DateTime? PostedAtUtc { get; private set; }

    private readonly List<JournalLine> _lines = new();
    public IReadOnlyCollection<JournalLine> Lines => _lines.AsReadOnly();

    public decimal TotalDebit => _lines.Sum(l => l.Debit);
    public decimal TotalCredit => _lines.Sum(l => l.Credit);
    public bool IsBalanced => TotalDebit == TotalCredit && TotalDebit > 0m;

    private JournalEntry() { }

    public static JournalEntry Create(
        string entryNumber,
        DateTime entryDate,
        VoucherType voucherType,
        string? reference = null,
        string? narration = null,
        string currency = "Tk")
    {
        if (string.IsNullOrWhiteSpace(entryNumber)) throw new ArgumentException("Entry number is required.", nameof(entryNumber));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));

        return new JournalEntry
        {
            EntryNumber = entryNumber.Trim().ToUpperInvariant(),
            EntryDate = entryDate,
            VoucherType = voucherType,
            Reference = Trim(reference),
            Narration = Trim(narration),
            Currency = currency.Trim(),
            Status = JournalStatus.Draft
        };
    }

    public JournalLine AddLine(Guid accountId, decimal debit, decimal credit, string? lineNarration = null)
    {
        if (Status != JournalStatus.Draft) throw new InvalidOperationException("Cannot modify a posted or void journal entry.");
        if (accountId == Guid.Empty) throw new ArgumentException("Account is required.", nameof(accountId));
        if (debit < 0m || credit < 0m) throw new ArgumentOutOfRangeException(nameof(debit), "Debit and credit cannot be negative.");
        if (debit > 0m == credit > 0m)
            throw new ArgumentException("Each line must have exactly one of debit or credit greater than zero.");

        var line = new JournalLine
        {
            JournalEntryId = Id,
            AccountId = accountId,
            Debit = debit,
            Credit = credit,
            LineNarration = Trim(lineNarration)
        };
        _lines.Add(line);
        return line;
    }

    public void UpdateDetails(DateTime entryDate, VoucherType voucherType, string? reference, string? narration)
    {
        if (Status != JournalStatus.Draft) throw new InvalidOperationException("Cannot modify a posted or void journal entry.");
        EntryDate = entryDate;
        VoucherType = voucherType;
        Reference = Trim(reference);
        Narration = Trim(narration);
    }

    /// <summary>Freeze a balanced draft. Enforces the double-entry invariant.</summary>
    public void Post(DateTime postedAtUtc)
    {
        if (Status == JournalStatus.Posted) throw new InvalidOperationException("Journal entry is already posted.");
        if (Status == JournalStatus.Void) throw new InvalidOperationException("Cannot post a void journal entry.");
        if (_lines.Count < 2) throw new InvalidOperationException("A journal entry needs at least two lines.");
        if (TotalDebit <= 0m) throw new InvalidOperationException("A journal entry must have a positive total.");
        if (TotalDebit != TotalCredit)
            throw new InvalidOperationException($"Journal entry is not balanced: debit {TotalDebit:0.00} != credit {TotalCredit:0.00}.");

        Status = JournalStatus.Posted;
        PostedAtUtc = postedAtUtc;
    }

    /// <summary>Reverse a posted (or abandon a draft) entry. Voided entries no longer count toward balances.</summary>
    public void Void()
    {
        if (Status == JournalStatus.Void) throw new InvalidOperationException("Journal entry is already void.");
        Status = JournalStatus.Void;
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
