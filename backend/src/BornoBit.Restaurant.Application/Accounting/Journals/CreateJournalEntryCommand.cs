using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Journals;

/// <summary>Creates a Draft journal entry. <paramref name="PostImmediately"/> posts it in the same call.</summary>
public record CreateJournalEntryCommand(
    DateTime EntryDate,
    VoucherType VoucherType,
    string? Reference,
    string? Narration,
    IReadOnlyList<JournalLineInput> Lines,
    bool PostImmediately = false) : IRequest<CreateJournalEntryResult>;

public record CreateJournalEntryResult(Guid Id, string EntryNumber, JournalStatus Status);

public class CreateJournalEntryCommandValidator : AbstractValidator<CreateJournalEntryCommand>
{
    public CreateJournalEntryCommandValidator()
    {
        RuleFor(x => x.VoucherType).IsInEnum();
        RuleFor(x => x.Reference).MaximumLength(80);
        RuleFor(x => x.Narration).MaximumLength(1000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A journal entry needs at least two lines.")
            .Must(l => l.Count >= 2).WithMessage("A journal entry needs at least two lines.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId).NotEmpty();
            line.RuleFor(l => l.Debit).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.Credit).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l)
                .Must(l => (l.Debit > 0m) ^ (l.Credit > 0m))
                .WithMessage("Each line must have exactly one of debit or credit greater than zero.");
        });

        RuleFor(x => x.Lines)
            .Must(lines => lines.Sum(l => l.Debit) == lines.Sum(l => l.Credit))
            .WithMessage("The entry is not balanced: total debits must equal total credits.")
            .Must(lines => lines.Sum(l => l.Debit) > 0m)
            .WithMessage("The entry total must be greater than zero.");
    }
}

public class CreateJournalEntryCommandHandler : IRequestHandler<CreateJournalEntryCommand, CreateJournalEntryResult>
{
    private readonly IAppDbContext _db;
    private readonly IJournalNumberGenerator _numbers;
    private readonly TimeProvider _timeProvider;

    public CreateJournalEntryCommandHandler(IAppDbContext db, IJournalNumberGenerator numbers, TimeProvider timeProvider)
    {
        _db = db;
        _numbers = numbers;
        _timeProvider = timeProvider;
    }

    public async Task<CreateJournalEntryResult> Handle(CreateJournalEntryCommand request, CancellationToken cancellationToken)
    {
        var accountIds = request.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _db.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .Select(a => new { a.Id, a.IsPostable, a.IsActive })
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        foreach (var id in accountIds)
        {
            if (!accounts.TryGetValue(id, out var acc))
                throw new NotFoundException($"Account {id} not found.");
            if (!acc.IsPostable)
                throw new ConflictException("Journal lines can only post to postable (leaf) accounts.");
            if (!acc.IsActive)
                throw new ConflictException("Journal lines cannot post to an inactive account.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var entryNumber = await _numbers.NextAsync(nowUtc, cancellationToken);

        var entry = JournalEntry.Create(entryNumber, request.EntryDate, request.VoucherType, request.Reference, request.Narration);
        foreach (var line in request.Lines)
            entry.AddLine(line.AccountId, line.Debit, line.Credit, line.Narration);

        if (request.PostImmediately)
            entry.Post(nowUtc);

        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        return new CreateJournalEntryResult(entry.Id, entry.EntryNumber, entry.Status);
    }
}
