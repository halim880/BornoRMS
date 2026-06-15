namespace BornoBit.Restaurant.Web.Components.Pages.Accounts;

/// <summary>Edit model for the "pay supplier" dialog.</summary>
public class SupplierPaymentFormModel
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = "";
    public decimal Outstanding { get; set; }

    public DateTime PaidOn { get; set; } = DateTime.UtcNow.Date;
    public decimal Amount { get; set; }
    public Guid? CashAccountId { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }

    /// <summary>Set once the payment is recorded, so the caller knows it succeeded.</summary>
    public bool Saved { get; set; }
}
