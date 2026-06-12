using BornoBit.Restaurant.Domain.Accounting;

namespace BornoBit.Restaurant.Web.Components.Pages.Accounts;

public class CashAccountFormModel
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CashAccountKind Kind { get; set; } = CashAccountKind.Cash;
    public decimal OpeningBalance { get; set; }

    public Guid? SavedId { get; set; }
}
