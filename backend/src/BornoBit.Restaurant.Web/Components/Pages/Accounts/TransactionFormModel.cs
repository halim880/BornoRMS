using BornoBit.Restaurant.Domain.Accounting;

namespace BornoBit.Restaurant.Web.Components.Pages.Accounts;

public class TransactionFormModel
{
    public Guid? Id { get; set; }
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow.Date;
    public TransactionType Type { get; set; } = TransactionType.Expense;
    public Guid? CashAccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public Guid? SavedId { get; set; }
}
