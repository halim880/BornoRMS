using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Store;

/// <summary>
/// A store issue: stock leaving the warehouse to a destination (kitchen / department / branch). Created as
/// <c>Draft</c>, then <c>Posted</c> — posting is the only path that lowers stock (the application layer calls
/// <c>StoreItem.Issue</c> per line and writes an <c>IssueOut</c> movement, valued at the item's current
/// average cost). <see cref="Destination"/> is free text — the issue does not post into the POS module.
/// </summary>
public class StoreIssue : AuditableEntity
{
    public string IssueNumber { get; private set; } = default!;
    public string Destination { get; private set; } = default!;
    public DateTime IssuedAtUtc { get; private set; }
    public string? Notes { get; private set; }
    public StoreIssueStatus Status { get; private set; } = StoreIssueStatus.Draft;
    public DateTime? PostedAtUtc { get; private set; }

    private readonly List<StoreIssueLine> _lines = new();
    public IReadOnlyCollection<StoreIssueLine> Lines => _lines.AsReadOnly();

    private StoreIssue() { }

    public static StoreIssue Create(
        string issueNumber,
        string destination,
        DateTime issuedAtUtc,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(issueNumber)) throw new ArgumentException("Issue number is required.", nameof(issueNumber));
        if (string.IsNullOrWhiteSpace(destination)) throw new ArgumentException("Destination is required.", nameof(destination));

        return new StoreIssue
        {
            IssueNumber = issueNumber.Trim().ToUpperInvariant(),
            Destination = destination.Trim(),
            IssuedAtUtc = issuedAtUtc,
            Notes = Trim(notes),
            Status = StoreIssueStatus.Draft
        };
    }

    public StoreIssueLine AddLine(Guid storeItemId, string itemName, decimal qty, Guid unitId, decimal qtyBase)
    {
        if (Status != StoreIssueStatus.Draft) throw new InvalidOperationException("Cannot modify a posted issue.");
        if (storeItemId == Guid.Empty) throw new ArgumentException("Item is required.", nameof(storeItemId));
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty), "Quantity must be positive.");
        if (unitId == Guid.Empty) throw new ArgumentException("Unit is required.", nameof(unitId));
        if (qtyBase <= 0) throw new ArgumentOutOfRangeException(nameof(qtyBase));

        var line = new StoreIssueLine
        {
            StoreIssueId = Id,
            StoreItemId = storeItemId,
            ItemName = itemName,
            Qty = qty,
            UnitId = unitId,
            QtyBase = qtyBase
        };
        _lines.Add(line);
        return line;
    }

    /// <summary>Mark the issue posted. The caller must have already applied stock effects for each line.</summary>
    public void MarkPosted(DateTime postedAtUtc)
    {
        if (Status == StoreIssueStatus.Posted) throw new InvalidOperationException("Issue is already posted.");
        if (_lines.Count == 0) throw new InvalidOperationException("Cannot post an issue with no lines.");
        Status = StoreIssueStatus.Posted;
        PostedAtUtc = postedAtUtc;
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
