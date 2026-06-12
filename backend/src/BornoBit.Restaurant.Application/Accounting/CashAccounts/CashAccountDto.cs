using BornoBit.Restaurant.Domain.Accounting;

namespace BornoBit.Restaurant.Application.Accounting.CashAccounts;

public record CashAccountDto(
    Guid Id,
    string Name,
    CashAccountKind Kind,
    decimal OpeningBalance,
    decimal Balance,
    bool IsActive);
