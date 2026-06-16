using BornoBit.Restaurant.Domain.Store;

namespace BornoBit.Restaurant.Web.Components.Pages.Store;

public class StorePayModel
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal Outstanding { get; set; }
    public decimal Amount { get; set; }
    public StorePaymentMethod Method { get; set; } = StorePaymentMethod.Cash;
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public bool Saved { get; set; }
}
