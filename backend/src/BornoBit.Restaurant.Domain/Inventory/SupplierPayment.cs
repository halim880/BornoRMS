using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Inventory;

/// <summary>
/// A payment made to a supplier against goods received. In this single-entry cash book a supplier
/// payment is the moment a purchase actually costs money: it is mirrored by a "Purchases" expense
/// <c>FinanceTransaction</c> drawn from <see cref="CashAccountId"/> (written together in the same
/// command). Outstanding payable for a supplier = Σ posted goods-receipt subtotals − Σ payments.
/// </summary>
public class SupplierPayment : AuditableEntity
{
    public Guid SupplierId { get; private set; }
    public Guid CashAccountId { get; private set; }
    public DateTime PaidOn { get; private set; }
    public decimal Amount { get; private set; }
    public string? Reference { get; private set; }
    public string? Notes { get; private set; }

    private SupplierPayment() { }

    public static SupplierPayment Create(
        Guid supplierId,
        Guid cashAccountId,
        DateTime paidOn,
        decimal amount,
        string? reference = null,
        string? notes = null)
    {
        if (supplierId == Guid.Empty) throw new ArgumentException("Supplier is required.", nameof(supplierId));
        if (cashAccountId == Guid.Empty) throw new ArgumentException("Cash account is required.", nameof(cashAccountId));
        if (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        return new SupplierPayment
        {
            SupplierId = supplierId,
            CashAccountId = cashAccountId,
            PaidOn = paidOn.Date,
            Amount = amount,
            Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
    }
}
