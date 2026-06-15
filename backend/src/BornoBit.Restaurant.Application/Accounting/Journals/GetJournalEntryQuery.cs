using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Journals;

public record GetJournalEntryQuery(Guid Id) : IRequest<JournalEntryDetailDto>;

public class GetJournalEntryQueryHandler : IRequestHandler<GetJournalEntryQuery, JournalEntryDetailDto>
{
    private readonly IAppDbContext _db;

    public GetJournalEntryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<JournalEntryDetailDto> Handle(GetJournalEntryQuery request, CancellationToken cancellationToken)
    {
        var entry = await _db.JournalEntries.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Journal entry not found.");

        var lines = await (
            from l in _db.JournalLines.AsNoTracking()
            join a in _db.Accounts.AsNoTracking() on l.AccountId equals a.Id
            where l.JournalEntryId == request.Id
            orderby a.Code
            select new JournalLineDto(l.Id, l.AccountId, a.Code, a.Name, l.Debit, l.Credit, l.LineNarration))
            .ToListAsync(cancellationToken);

        return new JournalEntryDetailDto(
            entry.Id,
            entry.EntryNumber,
            entry.EntryDate,
            entry.VoucherType,
            entry.Status,
            entry.Reference,
            entry.Narration,
            entry.Currency,
            entry.PostedAtUtc,
            lines.Sum(l => l.Debit),
            lines.Sum(l => l.Credit),
            lines);
    }
}
