using BornoBit.Restaurant.Domain.Accounting;

namespace BornoBit.Restaurant.Web.Components.Pages.Accounts;

public class AccountFormModel
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType AccountType { get; set; } = AccountType.Asset;
    public Guid? ParentId { get; set; }
    public bool IsPostable { get; set; } = true;
    public string? Description { get; set; }

    public Guid? SavedId { get; set; }
}
