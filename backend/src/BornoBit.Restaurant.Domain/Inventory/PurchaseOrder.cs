using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Inventory;

/// <summary>
/// A purchase order: a commitment to buy stock from a supplier. Created as <c>Draft</c>, then <c>Approved</c>.
/// Stock is never raised by a PO — goods receipts (GRNs) are raised against it, and posting a GRN bumps the
/// matched lines' <see cref="PurchaseOrderLine.QtyReceivedBase"/>, moving the PO through
/// <c>PartiallyReceived</c> → <c>Received</c>. A PO can be <c>Cancelled</c> before it is fully received.
/// </summary>
public class PurchaseOrder : AuditableEntity
{
    public string PoNumber { get; private set; } = default!;
    public Guid SupplierId { get; private set; }
    public DateTime OrderedAtUtc { get; private set; }
    public DateTime? ExpectedAtUtc { get; private set; }
    public string Currency { get; private set; } = "Tk";
    public string? Notes { get; private set; }
    public PurchaseOrderStatus Status { get; private set; } = PurchaseOrderStatus.Draft;
    public DateTime? ApprovedAtUtc { get; private set; }

    private readonly List<PurchaseOrderLine> _lines = new();
    public IReadOnlyCollection<PurchaseOrderLine> Lines => _lines.AsReadOnly();

    public decimal Subtotal => _lines.Sum(l => l.LineTotal);

    private PurchaseOrder() { }

    public static PurchaseOrder Create(
        string poNumber,
        Guid supplierId,
        DateTime orderedAtUtc,
        DateTime? expectedAtUtc = null,
        string currency = "Tk",
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(poNumber)) throw new ArgumentException("PO number is required.", nameof(poNumber));
        if (supplierId == Guid.Empty) throw new ArgumentException("Supplier is required.", nameof(supplierId));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));

        return new PurchaseOrder
        {
            PoNumber = poNumber.Trim().ToUpperInvariant(),
            SupplierId = supplierId,
            OrderedAtUtc = orderedAtUtc,
            ExpectedAtUtc = expectedAtUtc,
            Currency = currency.Trim(),
            Notes = Trim(notes),
            Status = PurchaseOrderStatus.Draft
        };
    }

    /// <summary>Edit the header of a Draft PO. Approved/received POs have an immutable header.</summary>
    public void UpdateHeader(Guid supplierId, DateTime orderedAtUtc, DateTime? expectedAtUtc, string? notes)
    {
        EnsureDraft("modify");
        if (supplierId == Guid.Empty) throw new ArgumentException("Supplier is required.", nameof(supplierId));

        SupplierId = supplierId;
        OrderedAtUtc = orderedAtUtc;
        ExpectedAtUtc = expectedAtUtc;
        Notes = Trim(notes);
    }

    /// <summary>Drop every line on a Draft PO so the caller can re-add them.</summary>
    public void ClearLines()
    {
        EnsureDraft("modify");
        _lines.Clear();
    }

    public PurchaseOrderLine AddLine(Guid inventoryItemId, string itemName, decimal qty, Guid unitId, decimal qtyBase, decimal unitCost)
    {
        EnsureDraft("modify");
        if (inventoryItemId == Guid.Empty) throw new ArgumentException("Item is required.", nameof(inventoryItemId));
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty), "Quantity must be positive.");
        if (unitId == Guid.Empty) throw new ArgumentException("Unit is required.", nameof(unitId));
        if (qtyBase <= 0) throw new ArgumentOutOfRangeException(nameof(qtyBase));
        if (unitCost < 0) throw new ArgumentOutOfRangeException(nameof(unitCost));

        var line = new PurchaseOrderLine
        {
            PurchaseOrderId = Id,
            InventoryItemId = inventoryItemId,
            ItemName = itemName,
            QtyOrdered = qty,
            UnitId = unitId,
            QtyOrderedBase = qtyBase,
            UnitCost = unitCost,
            QtyReceivedBase = 0m
        };
        _lines.Add(line);
        return line;
    }

    /// <summary>Approve a Draft PO so goods receipts can be raised against it.</summary>
    public void Approve(DateTime approvedAtUtc)
    {
        if (Status != PurchaseOrderStatus.Draft) throw new InvalidOperationException($"Only a draft purchase order can be approved (current: {Status}).");
        if (_lines.Count == 0) throw new InvalidOperationException("Cannot approve a purchase order with no lines.");
        Status = PurchaseOrderStatus.Approved;
        ApprovedAtUtc = approvedAtUtc;
    }

    /// <summary>Cancel a PO that is not already fully received.</summary>
    public void Cancel()
    {
        if (Status == PurchaseOrderStatus.Received) throw new InvalidOperationException("A fully received purchase order cannot be cancelled.");
        if (Status == PurchaseOrderStatus.Cancelled) throw new InvalidOperationException("Purchase order is already cancelled.");
        Status = PurchaseOrderStatus.Cancelled;
    }

    /// <summary>
    /// Match a posted goods receipt to a PO line: bump its received tally. Called once per GRN line at post
    /// time. Recompute the header status with <see cref="RecomputeStatus"/> after applying all of a GRN's lines.
    /// </summary>
    public void ApplyReceipt(Guid purchaseOrderLineId, decimal qtyBase)
    {
        if (Status is not (PurchaseOrderStatus.Approved or PurchaseOrderStatus.PartiallyReceived))
            throw new InvalidOperationException($"Goods can only be received against an approved purchase order (current: {Status}).");
        if (qtyBase <= 0) throw new ArgumentOutOfRangeException(nameof(qtyBase));

        var line = _lines.FirstOrDefault(l => l.Id == purchaseOrderLineId)
            ?? throw new InvalidOperationException($"Purchase order line {purchaseOrderLineId} is not part of '{PoNumber}'.");
        line.QtyReceivedBase += qtyBase;
    }

    /// <summary>Recompute the header status from the per-line received tallies (call after <see cref="ApplyReceipt"/>).</summary>
    public void RecomputeStatus()
    {
        if (Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Cancelled) return;
        if (_lines.Count == 0) return;

        if (_lines.All(l => l.IsFullyReceived))
            Status = PurchaseOrderStatus.Received;
        else if (_lines.Any(l => l.QtyReceivedBase > 0m))
            Status = PurchaseOrderStatus.PartiallyReceived;
        else
            Status = PurchaseOrderStatus.Approved;
    }

    private void EnsureDraft(string verb)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException($"Cannot {verb} a {Status} purchase order — only drafts are editable.");
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
