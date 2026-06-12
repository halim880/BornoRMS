using BornoBit.Restaurant.Domain.Accounting;

namespace BornoBit.Restaurant.Web.Components.Pages.Accounts;

public class CategoryFormModel
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TransactionType Type { get; set; } = TransactionType.Expense;

    public Guid? SavedId { get; set; }
}
