using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Store;

/// <summary>
/// A store issue: stock leaving the warehouse to a consuming department (Kitchen, Bar, Bakery…). Created as
/// <c>Draft</c>, then <c>Posted</c> — posting is the only path that lowers stock (the application layer calls
/// <c>StoreItem.Issue</c> per line and writes an <c>IssueOut</c> movement, valued at the item's current
/// average cost). <see cref="StoreDepartmentId"/> is the cost centre charged; <see cref="Destination"/> holds a
/// denormalised snapshot of the department name for display. <see cref="StoreRequisitionId"/> links the issue
/// back to an approved requisition when it was raised from one (null for ad-hoc issues).
/// </summary>
public class StoreIssue : AuditableEntity
{
    public string IssueNumber { get; private set; } = default!;
    public Guid StoreDepartmentId { get; private set; }
    public Guid? StoreRequisitionId { get; private set; }
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
        Guid storeDepartmentId,
        string departmentName,
        DateTime issuedAtUtc,
        string? notes = null,
        Guid? storeRequisitionId = null)
    {
        if (string.IsNullOrWhiteSpace(issueNumber)) throw new ArgumentException("Issue number is required.", nameof(issueNumber));
        if (storeDepartmentId == Guid.Empty) throw new ArgumentException("Department is required.", nameof(storeDepartmentId));
        if (string.IsNullOrWhiteSpace(departmentName)) throw new ArgumentException("Department name is required.", nameof(departmentName));

        return new StoreIssue
        {
            IssueNumber = issueNumber.Trim().ToUpperInvariant(),
            StoreDepartmentId = storeDepartmentId,
            StoreRequisitionId = storeRequisitionId,
            Destination = departmentName.Trim(),
            IssuedAtUtc = issuedAtUtc,
            Notes = Trim(notes),
            Status = StoreIssueStatus.Draft
        };
    }

    /// <summary>Edit the header fields of a draft issue. <paramref name="departmentName"/> is the snapshot stored
    /// in <see cref="Destination"/>.</summary>
    public void UpdateHeader(Guid storeDepartmentId, string departmentName, DateTime issuedAtUtc, string? notes, Guid? storeRequisitionId)
    {
        if (Status != StoreIssueStatus.Draft) throw new InvalidOperationException("Cannot edit a posted or voided issue.");
        if (storeDepartmentId == Guid.Empty) throw new ArgumentException("Department is required.", nameof(storeDepartmentId));
        if (string.IsNullOrWhiteSpace(departmentName)) throw new ArgumentException("Department name is required.", nameof(departmentName));

        StoreDepartmentId = storeDepartmentId;
        StoreRequisitionId = storeRequisitionId;
        Destination = departmentName.Trim();
        IssuedAtUtc = issuedAtUtc;
        Notes = Trim(notes);
    }

    /// <summary>Remove all lines from a draft issue (EF cascade-deletes the orphaned rows).</summary>
    public void ClearLines()
    {
        if (Status != StoreIssueStatus.Draft) throw new InvalidOperationException("Cannot modify a posted or voided issue.");
        _lines.Clear();
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

    /// <summary>Void a posted issue. The caller must have already restored the stock effects of each line.</summary>
    public void MarkVoided(DateTime voidedAtUtc)
    {
        if (Status != StoreIssueStatus.Posted) throw new InvalidOperationException("Only a posted issue can be voided.");
        Status = StoreIssueStatus.Voided;
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
