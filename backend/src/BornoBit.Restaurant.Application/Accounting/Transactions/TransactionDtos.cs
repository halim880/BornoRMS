using BornoBit.Restaurant.Domain.Accounting;

namespace BornoBit.Restaurant.Application.Accounting.Transactions;

/// <summary>A transaction row joined to its category and cash-account names, for the list.</summary>
public record TransactionListItemDto(
    Guid Id,
    string Number,
    DateTime OccurredOn,
    TransactionType Type,
    Guid CategoryId,
    string CategoryName,
    Guid CashAccountId,
    string CashAccountName,
    decimal Amount,
    string? Reference,
    string? Notes);
