using BornoBit.Restaurant.Domain.Accounting;

namespace BornoBit.Restaurant.Application.Accounting.Accounts;

public record AccountDto(
    Guid Id,
    string Code,
    string Name,
    AccountType AccountType,
    NormalBalance NormalBalance,
    Guid? ParentId,
    bool IsPostable,
    bool IsActive,
    string? Description);

/// <summary>An account plus its children, for the Chart of Accounts tree view.</summary>
public record AccountNodeDto(
    Guid Id,
    string Code,
    string Name,
    AccountType AccountType,
    NormalBalance NormalBalance,
    bool IsPostable,
    bool IsActive,
    IReadOnlyList<AccountNodeDto> Children);
