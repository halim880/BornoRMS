using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Inventory;

/// <summary>
/// Goods returned to a supplier (damaged / wrong / surplus). A return is posted immediately: stock is
/// issued out at the item's current moving-average cost (the per-item detail lives in
/// <see cref="StockMovement"/> rows of type <c>PurchaseReturn</c> referencing this header) and the
/// supplier payable is reduced — GL Dr Accounts Payable / Cr Purchases for <see cref="Subtotal"/>.
/// Outstanding payable for a supplier = Σ posted goods-receipt subtotals − Σ returns − Σ payments.
/// </summary>
public class PurchaseReturn : AuditableEntity
{
    public string ReturnNumber { get; private set; } = default!;
    public Guid SupplierId { get; private set; }
    public DateTime ReturnedAtUtc { get; private set; }
    public decimal Subtotal { get; private set; }
    public string? Reason { get; private set; }
    public string? Notes { get; private set; }

    private PurchaseReturn() { }

    public static PurchaseReturn Create(
        string returnNumber,
        Guid supplierId,
        DateTime returnedAtUtc,
        decimal subtotal,
        string? reason = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(returnNumber)) throw new ArgumentException("Return number is required.", nameof(returnNumber));
        if (supplierId == Guid.Empty) throw new ArgumentException("Supplier is required.", nameof(supplierId));
        if (subtotal <= 0m) throw new ArgumentOutOfRangeException(nameof(subtotal), "Return value must be greater than zero.");

        return new PurchaseReturn
        {
            ReturnNumber = returnNumber.Trim(),
            SupplierId = supplierId,
            ReturnedAtUtc = returnedAtUtc,
            Subtotal = subtotal,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
    }
}
