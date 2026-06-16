using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Store;

/// <summary>
/// A department store requisition: a request for stock raised by a consuming department (Kitchen, Bar…).
/// Workflow: <c>Draft</c> → <c>Submitted</c> → (<c>Approved</c> | <c>Rejected</c>). Approving optionally trims the
/// per-line quantity. Posted store issues linked to the requisition drive it <c>Approved</c> → <c>PartiallyIssued</c>
/// → <c>Issued</c> via <see cref="RecordIssued"/>. A Draft or Submitted requisition can be <c>Cancelled</c>.
/// Issuing actually lowers stock — the requisition itself never touches <c>StoreItem.QtyOnHand</c>.
/// </summary>
public class StoreRequisition : AuditableEntity
{
    public string RequisitionNumber { get; private set; } = default!;
    public Guid StoreDepartmentId { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? RequiredByUtc { get; private set; }
    public string? Notes { get; private set; }
    public StoreRequisitionStatus Status { get; private set; } = StoreRequisitionStatus.Draft;
    public DateTime? ApprovedAtUtc { get; private set; }
    public string? RejectedReason { get; private set; }

    private readonly List<StoreRequisitionLine> _lines = new();
    public IReadOnlyCollection<StoreRequisitionLine> Lines => _lines.AsReadOnly();

    private StoreRequisition() { }

    public static StoreRequisition Create(
        string requisitionNumber,
        Guid storeDepartmentId,
        DateTime requestedAtUtc,
        DateTime? requiredByUtc = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(requisitionNumber)) throw new ArgumentException("Requisition number is required.", nameof(requisitionNumber));
        if (storeDepartmentId == Guid.Empty) throw new ArgumentException("Department is required.", nameof(storeDepartmentId));

        return new StoreRequisition
        {
            RequisitionNumber = requisitionNumber.Trim().ToUpperInvariant(),
            StoreDepartmentId = storeDepartmentId,
            RequestedAtUtc = requestedAtUtc,
            RequiredByUtc = requiredByUtc,
            Notes = Trim(notes),
            Status = StoreRequisitionStatus.Draft
        };
    }

    /// <summary>Edit the header of a draft requisition.</summary>
    public void UpdateHeader(Guid storeDepartmentId, DateTime? requiredByUtc, string? notes)
    {
        if (Status != StoreRequisitionStatus.Draft) throw new InvalidOperationException("Only a draft requisition can be edited.");
        if (storeDepartmentId == Guid.Empty) throw new ArgumentException("Department is required.", nameof(storeDepartmentId));
        StoreDepartmentId = storeDepartmentId;
        RequiredByUtc = requiredByUtc;
        Notes = Trim(notes);
    }

    public void ClearLines()
    {
        if (Status != StoreRequisitionStatus.Draft) throw new InvalidOperationException("Only a draft requisition can be modified.");
        _lines.Clear();
    }

    public StoreRequisitionLine AddLine(Guid storeItemId, string itemName, decimal requestedQty, Guid unitId, decimal requestedQtyBase)
    {
        if (Status != StoreRequisitionStatus.Draft) throw new InvalidOperationException("Only a draft requisition can be modified.");
        if (storeItemId == Guid.Empty) throw new ArgumentException("Item is required.", nameof(storeItemId));
        if (requestedQty <= 0) throw new ArgumentOutOfRangeException(nameof(requestedQty), "Quantity must be positive.");
        if (unitId == Guid.Empty) throw new ArgumentException("Unit is required.", nameof(unitId));
        if (requestedQtyBase <= 0) throw new ArgumentOutOfRangeException(nameof(requestedQtyBase));

        var line = new StoreRequisitionLine
        {
            StoreRequisitionId = Id,
            StoreItemId = storeItemId,
            ItemName = itemName,
            RequestedQty = requestedQty,
            UnitId = unitId,
            RequestedQtyBase = requestedQtyBase,
            ApprovedQtyBase = 0m,
            IssuedQtyBase = 0m
        };
        _lines.Add(line);
        return line;
    }

    public void Submit()
    {
        if (Status != StoreRequisitionStatus.Draft) throw new InvalidOperationException("Only a draft requisition can be submitted.");
        if (_lines.Count == 0) throw new InvalidOperationException("Cannot submit a requisition with no lines.");
        Status = StoreRequisitionStatus.Submitted;
    }

    /// <summary>Approve a submitted requisition. <paramref name="approvedQtyBaseByLineId"/> overrides the approved
    /// quantity per line (capped at the requested quantity); any line not present defaults to its requested quantity.</summary>
    public void Approve(IReadOnlyDictionary<Guid, decimal> approvedQtyBaseByLineId, DateTime approvedAtUtc)
    {
        if (Status != StoreRequisitionStatus.Submitted) throw new InvalidOperationException("Only a submitted requisition can be approved.");

        foreach (var line in _lines)
        {
            var approved = approvedQtyBaseByLineId is not null && approvedQtyBaseByLineId.TryGetValue(line.Id, out var q)
                ? q
                : line.RequestedQtyBase;
            if (approved < 0) throw new ArgumentOutOfRangeException(nameof(approvedQtyBaseByLineId), "Approved quantity cannot be negative.");
            line.ApprovedQtyBase = Math.Min(approved, line.RequestedQtyBase);
        }

        Status = StoreRequisitionStatus.Approved;
        ApprovedAtUtc = approvedAtUtc;
    }

    public void Reject(string reason)
    {
        if (Status != StoreRequisitionStatus.Submitted) throw new InvalidOperationException("Only a submitted requisition can be rejected.");
        Status = StoreRequisitionStatus.Rejected;
        RejectedReason = Trim(reason);
    }

    public void Cancel()
    {
        if (Status is not (StoreRequisitionStatus.Draft or StoreRequisitionStatus.Submitted))
            throw new InvalidOperationException("Only a draft or submitted requisition can be cancelled.");
        Status = StoreRequisitionStatus.Cancelled;
    }

    /// <summary>Apply (or, with a negative delta, reverse) issued quantity for a line, then recompute the header
    /// status. Called by the store-issue post/void handlers so requisition progress mirrors the posted issues.</summary>
    public void RecordIssued(Guid lineId, decimal deltaBase)
    {
        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new InvalidOperationException($"Requisition line {lineId} not found.");

        var newIssued = line.IssuedQtyBase + deltaBase;
        line.IssuedQtyBase = newIssued < 0 ? 0m : newIssued;

        RecomputeStatus();
    }

    private void RecomputeStatus()
    {
        // Only meaningful once the requisition has been approved/issued; ignore otherwise.
        if (Status is not (StoreRequisitionStatus.Approved or StoreRequisitionStatus.PartiallyIssued or StoreRequisitionStatus.Issued))
            return;

        var anyIssued = _lines.Any(l => l.IssuedQtyBase > 0m);
        var allFullyIssued = _lines.All(l => l.IssuedQtyBase >= l.ApprovedQtyBase);

        Status = !anyIssued ? StoreRequisitionStatus.Approved
            : allFullyIssued ? StoreRequisitionStatus.Issued
            : StoreRequisitionStatus.PartiallyIssued;
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
