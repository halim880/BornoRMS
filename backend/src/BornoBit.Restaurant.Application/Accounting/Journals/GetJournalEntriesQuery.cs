using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Journals;

public record GetJournalEntriesQuery(
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    VoucherType? VoucherType = null,
    JournalStatus? Status = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<JournalEntryListItemDto>>;

public class GetJournalEntriesQueryHandler
    : IRequestHandler<GetJournalEntriesQuery, PagedResult<JournalEntryListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetJournalEntriesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<JournalEntryListItemDto>> Handle(GetJournalEntriesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = _db.JournalEntries.AsNoTracking();

        if (request.FromDate is { } from) query = query.Where(e => e.EntryDate >= from);
        if (request.ToDate is { } to) query = query.Where(e => e.EntryDate < to.Date.AddDays(1));
        if (request.VoucherType is { } vt) query = query.Where(e => e.VoucherType == vt);
        if (request.Status is { } st) query = query.Where(e => e.Status == st);

        var total = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.EntryDate).ThenByDescending(e => e.EntryNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new JournalEntryListItemDto(
                e.Id,
                e.EntryNumber,
                e.EntryDate,
                e.VoucherType,
                e.Status,
                e.Reference,
                e.Narration,
                e.Lines.Sum(l => (decimal?)l.Debit) ?? 0m,
                e.Lines.Sum(l => (decimal?)l.Credit) ?? 0m,
                e.Lines.Count()))
            .ToListAsync(cancellationToken);

        return new PagedResult<JournalEntryListItemDto>(items, page, pageSize, total);
    }
}
