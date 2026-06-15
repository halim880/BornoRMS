using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Inventory;

/// <summary>
/// A goods receipt note (GRN): one delivery of stock from a supplier. Created as <c>Draft</c>, then
/// <c>Posted</c> — posting is the only path that raises stock (the application layer converts each line to
/// base units, calls <c>InventoryItem.Receive</c> and writes a <c>PurchaseIn</c> movement). <see cref="InvoiceNo"/>
/// is nullable because cash kacha-bazar buying often has no invoice.
/// </summary>
public class GoodsReceipt : AuditableEntity
{
    public string GrnNumber { get; private set; } = default!;
    public Guid SupplierId { get; private set; }
    public string? InvoiceNo { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }
    public string Currency { get; private set; } = "Tk";
    public string? Notes { get; private set; }
    public GoodsReceiptStatus Status { get; private set; } = GoodsReceiptStatus.Draft;
    public DateTime? PostedAtUtc { get; private set; }

    private readonly List<GoodsReceiptLine> _lines = new();
    public IReadOnlyCollection<GoodsReceiptLine> Lines => _lines.AsReadOnly();

    public decimal Subtotal => _lines.Sum(l => l.LineTotal);

    private GoodsReceipt() { }

    public static GoodsReceipt Create(
        string grnNumber,
        Guid supplierId,
        DateTime receivedAtUtc,
        string? invoiceNo = null,
        string currency = "Tk",
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(grnNumber)) throw new ArgumentException("GRN number is required.", nameof(grnNumber));
        if (supplierId == Guid.Empty) throw new ArgumentException("Supplier is required.", nameof(supplierId));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));

        return new GoodsReceipt
        {
            GrnNumber = grnNumber.Trim().ToUpperInvariant(),
            SupplierId = supplierId,
            ReceivedAtUtc = receivedAtUtc,
            InvoiceNo = Trim(invoiceNo),
            Currency = currency.Trim(),
            Notes = Trim(notes),
            Status = GoodsReceiptStatus.Draft
        };
    }

    /// <summary>Edit the header of a draft. Posted receipts are immutable.</summary>
    public void UpdateHeader(Guid supplierId, DateTime receivedAtUtc, string? invoiceNo, string? notes)
    {
        if (Status != GoodsReceiptStatus.Draft) throw new InvalidOperationException("Cannot modify a posted goods receipt.");
        if (supplierId == Guid.Empty) throw new ArgumentException("Supplier is required.", nameof(supplierId));

        SupplierId = supplierId;
        ReceivedAtUtc = receivedAtUtc;
        InvoiceNo = Trim(invoiceNo);
        Notes = Trim(notes);
    }

    /// <summary>Drop every line on a draft so the caller can re-add them. Posted receipts are immutable.</summary>
    public void ClearLines()
    {
        if (Status != GoodsReceiptStatus.Draft) throw new InvalidOperationException("Cannot modify a posted goods receipt.");
        _lines.Clear();
    }

    public GoodsReceiptLine AddLine(Guid inventoryItemId, string itemName, decimal qty, Guid unitId, decimal qtyBase, decimal unitCost)
    {
        if (Status != GoodsReceiptStatus.Draft) throw new InvalidOperationException("Cannot modify a posted goods receipt.");
        if (inventoryItemId == Guid.Empty) throw new ArgumentException("Item is required.", nameof(inventoryItemId));
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty), "Quantity must be positive.");
        if (unitId == Guid.Empty) throw new ArgumentException("Unit is required.", nameof(unitId));
        if (qtyBase <= 0) throw new ArgumentOutOfRangeException(nameof(qtyBase));
        if (unitCost < 0) throw new ArgumentOutOfRangeException(nameof(unitCost));

        var line = new GoodsReceiptLine
        {
            GoodsReceiptId = Id,
            InventoryItemId = inventoryItemId,
            ItemName = itemName,
            Qty = qty,
            UnitId = unitId,
            QtyBase = qtyBase,
            UnitCost = unitCost
        };
        _lines.Add(line);
        return line;
    }

    /// <summary>Mark the receipt posted. The caller must have already applied stock effects for each line.</summary>
    public void MarkPosted(DateTime postedAtUtc)
    {
        if (Status == GoodsReceiptStatus.Posted) throw new InvalidOperationException("Goods receipt is already posted.");
        if (_lines.Count == 0) throw new InvalidOperationException("Cannot post a goods receipt with no lines.");
        Status = GoodsReceiptStatus.Posted;
        PostedAtUtc = postedAtUtc;
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
