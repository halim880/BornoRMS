using BornoBit.Restaurant.Domain.Accounting;

namespace BornoBit.Restaurant.Application.Accounting.Journals;

/// <summary>One line of a journal entry on the way in. Exactly one of Debit/Credit is &gt; 0.</summary>
public record JournalLineInput(Guid AccountId, decimal Debit, decimal Credit, string? Narration);

public record JournalLineDto(
    Guid Id,
    Guid AccountId,
    string AccountCode,
    string AccountName,
    decimal Debit,
    decimal Credit,
    string? Narration);

public record JournalEntryListItemDto(
    Guid Id,
    string EntryNumber,
    DateTime EntryDate,
    VoucherType VoucherType,
    JournalStatus Status,
    string? Reference,
    string? Narration,
    decimal TotalDebit,
    decimal TotalCredit,
    int LineCount);

public record JournalEntryDetailDto(
    Guid Id,
    string EntryNumber,
    DateTime EntryDate,
    VoucherType VoucherType,
    JournalStatus Status,
    string? Reference,
    string? Narration,
    string Currency,
    DateTime? PostedAtUtc,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyList<JournalLineDto> Lines);
