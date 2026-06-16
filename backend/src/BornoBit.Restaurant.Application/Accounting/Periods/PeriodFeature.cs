using BornoBit.Restaurant.Application.Accounting.Posting;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Periods;

public record FiscalPeriodDto(Guid Id, int Year, int Month, string Name, FiscalPeriodStatus Status, DateTime? ClosedAtUtc, string? ClosedBy);

/// <summary>All fiscal periods that have a row, newest first. Months with no row are implicitly open.</summary>
public record GetPeriodsQuery : IRequest<IReadOnlyList<FiscalPeriodDto>>;

public class GetPeriodsQueryHandler : IRequestHandler<GetPeriodsQuery, IReadOnlyList<FiscalPeriodDto>>
{
    private readonly IAppDbContext _db;
    public GetPeriodsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<FiscalPeriodDto>> Handle(GetPeriodsQuery request, CancellationToken cancellationToken) =>
        await _db.FiscalPeriods
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .Select(p => new FiscalPeriodDto(p.Id, p.Year, p.Month, p.Name, p.Status, p.ClosedAtUtc, p.ClosedBy))
            .ToListAsync(cancellationToken);
}

/// <summary>Close a calendar month: postings dated in it are then rejected. Blocks if draft journals linger.</summary>
public record ClosePeriodCommand(int Year, int Month) : IRequest<Unit>;

public class ClosePeriodCommandValidator : AbstractValidator<ClosePeriodCommand>
{
    public ClosePeriodCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 9999);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

public class ClosePeriodCommandHandler : IRequestHandler<ClosePeriodCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _time;
    private readonly ICurrentUser _currentUser;

    public ClosePeriodCommandHandler(IAppDbContext db, TimeProvider time, ICurrentUser currentUser)
    {
        _db = db;
        _time = time;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(ClosePeriodCommand request, CancellationToken cancellationToken)
    {
        var draftsInMonth = await _db.JournalEntries.CountAsync(
            e => e.Status == JournalStatus.Draft && e.EntryDate.Year == request.Year && e.EntryDate.Month == request.Month,
            cancellationToken);
        if (draftsInMonth > 0)
            throw new ConflictException($"There are {draftsInMonth} draft journal entr(ies) dated in {request.Year}-{request.Month:00}. Post or void them before closing.");

        var period = await _db.FiscalPeriods.FirstOrDefaultAsync(p => p.Year == request.Year && p.Month == request.Month, cancellationToken);
        if (period is null)
        {
            period = FiscalPeriod.Create(request.Year, request.Month);
            _db.FiscalPeriods.Add(period);
        }

        try { period.Close(_time.GetUtcNow().UtcDateTime, _currentUser.UserName); }
        catch (InvalidOperationException ex) { throw new ConflictException(ex.Message); }

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

/// <summary>Reopen a closed month (Admin). Postings dated in it are accepted again.</summary>
public record ReopenPeriodCommand(int Year, int Month) : IRequest<Unit>;

public class ReopenPeriodCommandHandler : IRequestHandler<ReopenPeriodCommand, Unit>
{
    private readonly IAppDbContext _db;
    public ReopenPeriodCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(ReopenPeriodCommand request, CancellationToken cancellationToken)
    {
        var period = await _db.FiscalPeriods.FirstOrDefaultAsync(p => p.Year == request.Year && p.Month == request.Month, cancellationToken)
            ?? throw new NotFoundException($"Period {request.Year}-{request.Month:00} not found.");

        try { period.Reopen(); }
        catch (InvalidOperationException ex) { throw new ConflictException(ex.Message); }

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

/// <summary>
/// Year-end close: zero every income/expense account (cumulative through 31 Dec of the year) into Retained
/// Earnings via one balanced closing journal, then close all 12 months. Idempotent on reference YEARCLOSE-&lt;year&gt;.
/// </summary>
public record CloseYearCommand(int Year) : IRequest<CloseYearResultDto>;

public record CloseYearResultDto(decimal NetProfit, string? JournalEntryNumber, bool AlreadyClosed);

public class CloseYearCommandValidator : AbstractValidator<CloseYearCommand>
{
    public CloseYearCommandValidator() => RuleFor(x => x.Year).InclusiveBetween(2000, 9999);
}

public class CloseYearCommandHandler : IRequestHandler<CloseYearCommand, CloseYearResultDto>
{
    private readonly IAppDbContext _db;
    private readonly IGeneralLedgerService _gl;
    private readonly TimeProvider _time;
    private readonly ICurrentUser _currentUser;

    public CloseYearCommandHandler(IAppDbContext db, IGeneralLedgerService gl, TimeProvider time, ICurrentUser currentUser)
    {
        _db = db;
        _gl = gl;
        _time = time;
        _currentUser = currentUser;
    }

    public async Task<CloseYearResultDto> Handle(CloseYearCommand request, CancellationToken cancellationToken)
    {
        var reference = $"YEARCLOSE-{request.Year}";
        if (await _db.JournalEntries.AnyAsync(e => e.Reference == reference && e.Status != JournalStatus.Void, cancellationToken))
            return new CloseYearResultDto(0m, null, AlreadyClosed: true);

        var yearEnd = new DateTime(request.Year, 12, 31);

        // Net posted balance per P&L account, cumulative through year-end. Income is credit-normal, expense debit-normal.
        var balances = await (
            from l in _db.JournalLines
            join e in _db.JournalEntries on l.JournalEntryId equals e.Id
            join a in _db.Accounts on l.AccountId equals a.Id
            where e.Status == JournalStatus.Posted && e.EntryDate <= yearEnd
                  && (a.AccountType == AccountType.Income || a.AccountType == AccountType.Expense)
            group new { l.Debit, l.Credit, a.AccountType } by new { l.AccountId, a.AccountType } into g
            select new
            {
                g.Key.AccountId,
                g.Key.AccountType,
                Debit = g.Sum(x => x.Debit),
                Credit = g.Sum(x => x.Credit)
            }).ToListAsync(cancellationToken);

        var lines = new List<GlPostingLine>();
        decimal incomeTotal = 0m, expenseTotal = 0m;

        foreach (var b in balances)
        {
            if (b.AccountType == AccountType.Income)
            {
                var net = b.Credit - b.Debit; // credit-normal
                if (net == 0m) continue;
                incomeTotal += net;
                // Close a credit-balance income account by debiting it.
                lines.Add(net > 0m
                    ? GlPostingLine.DrId(b.AccountId, net, "Close income to retained earnings")
                    : GlPostingLine.CrId(b.AccountId, -net, "Close income to retained earnings"));
            }
            else
            {
                var net = b.Debit - b.Credit; // debit-normal
                if (net == 0m) continue;
                expenseTotal += net;
                // Close a debit-balance expense account by crediting it.
                lines.Add(net > 0m
                    ? GlPostingLine.CrId(b.AccountId, net, "Close expense to retained earnings")
                    : GlPostingLine.DrId(b.AccountId, -net, "Close expense to retained earnings"));
            }
        }

        var netProfit = incomeTotal - expenseTotal;
        if (lines.Count == 0)
            return new CloseYearResultDto(0m, null, AlreadyClosed: false);

        // Balancing line to Retained Earnings: credit for a profit, debit for a loss.
        lines.Add(netProfit >= 0m
            ? GlPostingLine.Cr(GlCodes.RetainedEarnings, netProfit, $"Net profit {request.Year}")
            : GlPostingLine.Dr(GlCodes.RetainedEarnings, -netProfit, $"Net loss {request.Year}"));

        var entry = await _gl.PostAsync(_db, yearEnd, VoucherType.Journal, lines, reference, $"Year-end close {request.Year}", cancellationToken);

        // Close all 12 months of the year.
        var existing = await _db.FiscalPeriods.Where(p => p.Year == request.Year).ToListAsync(cancellationToken);
        var byMonth = existing.ToDictionary(p => p.Month);
        var nowUtc = _time.GetUtcNow().UtcDateTime;
        for (var m = 1; m <= 12; m++)
        {
            if (!byMonth.TryGetValue(m, out var period))
            {
                period = FiscalPeriod.Create(request.Year, m);
                _db.FiscalPeriods.Add(period);
            }
            if (period.Status == FiscalPeriodStatus.Open) period.Close(nowUtc, _currentUser.UserName);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new CloseYearResultDto(netProfit, entry.EntryNumber, AlreadyClosed: false);
    }
}
