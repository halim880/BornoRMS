using BornoBit.Restaurant.Domain.Accounting;

namespace BornoBit.Restaurant.Web.Components.Pages.Accounts.Gl;

/// <summary>Edit model for the chart-of-accounts add/edit dialog.</summary>
public class AccountFormModel
{
    public Guid? Id { get; set; }                 // null = create
    public string Code { get; set; } = "";        // immutable once created
    public string Name { get; set; } = "";
    public AccountType AccountType { get; set; } = AccountType.Asset;
    public Guid? ParentId { get; set; }
    public bool IsPostable { get; set; } = true;
    public string? Description { get; set; }

    public bool Saved { get; set; }
}
