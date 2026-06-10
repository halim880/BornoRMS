using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Store;

/// <summary>
/// A store goods receipt: one delivery of stock from a supplier into the warehouse. Created as <c>Draft</c>,
/// then <c>Posted</c> — posting is the only path that raises stock (the application layer converts each line to
/// base units, calls <c>StoreItem.Receive</c> and writes a <c>PurchaseIn</c> movement).
/// </summary>
public class StoreGoodsReceipt : AuditableEntity
{
    public string GrnNumber { get; private set; } = default!;
    public Guid StoreSupplierId { get; private set; }
    public string? InvoiceNo { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }
    public string Currency { get; private set; } = "Tk";
    public string? Notes { get; private set; }
    public StoreGoodsReceiptStatus Status { get; private set; } = StoreGoodsReceiptStatus.Draft;
    public DateTime? PostedAtUtc { get; private set; }

    private readonly List<StoreGoodsReceiptLine> _lines = new();
    public IReadOnlyCollection<StoreGoodsReceiptLine> Lines => _lines.AsReadOnly();

    public decimal Subtotal => _lines.Sum(l => l.LineTotal);

    private StoreGoodsReceipt() { }

    public static StoreGoodsReceipt Create(
        string grnNumber,
        Guid storeSupplierId,
        DateTime receivedAtUtc,
        string? invoiceNo = null,
        string currency = "Tk",
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(grnNumber)) throw new ArgumentException("GRN number is required.", nameof(grnNumber));
        if (storeSupplierId == Guid.Empty) throw new ArgumentException("Supplier is required.", nameof(storeSupplierId));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));

        return new StoreGoodsReceipt
        {
            GrnNumber = grnNumber.Trim().ToUpperInvariant(),
            StoreSupplierId = storeSupplierId,
            ReceivedAtUtc = receivedAtUtc,
            InvoiceNo = Trim(invoiceNo),
            Currency = currency.Trim(),
            Notes = Trim(notes),
            Status = StoreGoodsReceiptStatus.Draft
        };
    }

    public StoreGoodsReceiptLine AddLine(Guid storeItemId, string itemName, decimal qty, Guid unitId, decimal qtyBase, decimal unitCost)
    {
        if (Status != StoreGoodsReceiptStatus.Draft) throw new InvalidOperationException("Cannot modify a posted goods receipt.");
        if (storeItemId == Guid.Empty) throw new ArgumentException("Item is required.", nameof(storeItemId));
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty), "Quantity must be positive.");
        if (unitId == Guid.Empty) throw new ArgumentException("Unit is required.", nameof(unitId));
        if (qtyBase <= 0) throw new ArgumentOutOfRangeException(nameof(qtyBase));
        if (unitCost < 0) throw new ArgumentOutOfRangeException(nameof(unitCost));

        var line = new StoreGoodsReceiptLine
        {
            StoreGoodsReceiptId = Id,
            StoreItemId = storeItemId,
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
        if (Status == StoreGoodsReceiptStatus.Posted) throw new InvalidOperationException("Goods receipt is already posted.");
        if (_lines.Count == 0) throw new InvalidOperationException("Cannot post a goods receipt with no lines.");
        Status = StoreGoodsReceiptStatus.Posted;
        PostedAtUtc = postedAtUtc;
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
