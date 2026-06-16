using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Store;

/// <summary>How a store supplier payment was settled.</summary>
public enum StorePaymentMethod
{
    Cash = 1,
    Bank = 2,
    MobileBanking = 3,
    Cheque = 4,
    Other = 5
}

/// <summary>
/// A payment made to a store/warehouse supplier against goods purchased. The supplier's outstanding
/// balance is derived: Σ posted goods-receipt subtotals − Σ payments. Isolated from the POS SupplierPayment.
/// </summary>
public class StorePayment : AuditableEntity
{
    public Guid StoreSupplierId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PaidAtUtc { get; private set; }
    public StorePaymentMethod Method { get; private set; }
    public string? Reference { get; private set; }
    public string? Notes { get; private set; }

    private StorePayment() { }

    public static StorePayment Create(
        Guid storeSupplierId,
        decimal amount,
        DateTime paidAtUtc,
        StorePaymentMethod method,
        string? reference = null,
        string? notes = null)
    {
        if (storeSupplierId == Guid.Empty) throw new ArgumentException("Supplier is required.", nameof(storeSupplierId));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be positive.");

        return new StorePayment
        {
            StoreSupplierId = storeSupplierId,
            Amount = amount,
            PaidAtUtc = paidAtUtc,
            Method = method,
            Reference = Trim(reference),
            Notes = Trim(notes)
        };
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
