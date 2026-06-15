namespace BornoBit.Restaurant.Domain.Accounting;

/// <summary>
/// Classifies a journal entry. Phase 1 records the type for filtering/reporting; the
/// convenience voucher forms (Payment/Receipt/Contra) arrive in phase 2.
/// </summary>
public enum VoucherType
{
    Journal = 1,
    Payment = 2,
    Receipt = 3,
    Contra = 4
}
