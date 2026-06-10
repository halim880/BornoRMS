using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Accounting;

/// <summary>
/// A node in the Chart of Accounts. Accounts form a tree via <see cref="ParentId"/>;
/// only <see cref="IsPostable"/> leaf accounts may carry journal lines (group/header
/// accounts exist purely to roll up their children). <see cref="NormalBalance"/> is
/// derived from <see cref="AccountType"/> and governs the sign of ledger balances.
/// </summary>
public class Account : AuditableEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public AccountType AccountType { get; private set; }
    public Guid? ParentId { get; private set; }
    public bool IsPostable { get; private set; } = true;
    public bool IsActive { get; private set; } = true;
    public string? Description { get; private set; }

    /// <summary>Derived from <see cref="AccountType"/>; never stored (EF-ignored).</summary>
    public NormalBalance NormalBalance => AccountType is AccountType.Asset or AccountType.Expense
        ? NormalBalance.Debit
        : NormalBalance.Credit;

    private Account() { }

    public static Account Create(
        string code,
        string name,
        AccountType accountType,
        Guid? parentId = null,
        bool isPostable = true,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Account code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Account name is required.", nameof(name));

        return new Account
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            AccountType = accountType,
            ParentId = parentId,
            IsPostable = isPostable,
            Description = Trim(description),
            IsActive = true
        };
    }

    public void Update(string name, AccountType accountType, Guid? parentId, bool isPostable, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Account name is required.", nameof(name));
        if (parentId == Id) throw new InvalidOperationException("An account cannot be its own parent.");

        Name = name.Trim();
        AccountType = accountType;
        ParentId = parentId;
        IsPostable = isPostable;
        Description = Trim(description);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
