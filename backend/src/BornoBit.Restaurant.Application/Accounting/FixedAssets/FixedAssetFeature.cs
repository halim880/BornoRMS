using BornoBit.Restaurant.Application.Accounting.Posting;
using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Domain.FixedAssets;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.FixedAssets;

public record FixedAssetDto(
    Guid Id, string AssetNumber, string Name, string GlAccountName, DateTime AcquisitionDate,
    decimal Cost, decimal SalvageValue, int UsefulLifeMonths, decimal AccumulatedDepreciation,
    decimal NetBookValue, FixedAssetStatus Status);

/// <summary>Register a fixed asset (no acquisition journal — the asset was already booked when purchased).</summary>
public record CreateFixedAssetCommand(
    string Name, Guid AssetGlAccountId, DateTime AcquisitionDate, decimal Cost, decimal SalvageValue, int UsefulLifeMonths)
    : IRequest<Guid>;

public class CreateFixedAssetCommandValidator : AbstractValidator<CreateFixedAssetCommand>
{
    public CreateFixedAssetCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AssetGlAccountId).NotEmpty();
        RuleFor(x => x.Cost).GreaterThan(0);
        RuleFor(x => x.SalvageValue).GreaterThanOrEqualTo(0).LessThan(x => x.Cost)
            .WithMessage("Salvage value must be between 0 and the cost.");
        RuleFor(x => x.UsefulLifeMonths).GreaterThan(0);
    }
}

public class CreateFixedAssetCommandHandler : IRequestHandler<CreateFixedAssetCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly IAssetNumberGenerator _numbers;
    private readonly TimeProvider _time;

    public CreateFixedAssetCommandHandler(IAppDbContext db, IAssetNumberGenerator numbers, TimeProvider time)
    {
        _db = db;
        _numbers = numbers;
        _time = time;
    }

    public async Task<Guid> Handle(CreateFixedAssetCommand request, CancellationToken cancellationToken)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == request.AssetGlAccountId, cancellationToken)
            ?? throw new NotFoundException("GL asset account not found.");
        if (account.AccountType != AccountType.Asset || !account.IsPostable)
            throw new ConflictException("The selected account must be a postable asset account.");

        var number = await _numbers.NextAsync(_time.GetUtcNow().UtcDateTime, cancellationToken);
        var asset = FixedAsset.Create(number, request.Name, request.AssetGlAccountId, request.AcquisitionDate,
            request.Cost, request.SalvageValue, request.UsefulLifeMonths);
        _db.FixedAssets.Add(asset);
        await _db.SaveChangesAsync(cancellationToken);
        return asset.Id;
    }
}

public record GetFixedAssetsQuery(bool ActiveOnly = false) : IRequest<IReadOnlyList<FixedAssetDto>>;

public class GetFixedAssetsQueryHandler : IRequestHandler<GetFixedAssetsQuery, IReadOnlyList<FixedAssetDto>>
{
    private readonly IAppDbContext _db;
    public GetFixedAssetsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<FixedAssetDto>> Handle(GetFixedAssetsQuery request, CancellationToken cancellationToken)
    {
        var query =
            from a in _db.FixedAssets
            join acc in _db.Accounts on a.AssetGlAccountId equals acc.Id into accs
            from acc in accs.DefaultIfEmpty()
            select new { a, GlName = acc != null ? acc.Name : "—" };

        if (request.ActiveOnly)
            query = query.Where(x => x.a.Status == FixedAssetStatus.Active);

        return await query
            .OrderBy(x => x.a.AssetNumber)
            .Select(x => new FixedAssetDto(
                x.a.Id, x.a.AssetNumber, x.a.Name, x.GlName, x.a.AcquisitionDate,
                x.a.Cost, x.a.SalvageValue, x.a.UsefulLifeMonths, x.a.AccumulatedDepreciation,
                x.a.Cost - x.a.AccumulatedDepreciation, x.a.Status))
            .ToListAsync(cancellationToken);
    }
}

public record DepreciationScheduleRow(string AssetNumber, string AssetName, int Year, int Month, decimal Amount);

public record GetDepreciationScheduleQuery : IRequest<IReadOnlyList<DepreciationScheduleRow>>;

public class GetDepreciationScheduleQueryHandler : IRequestHandler<GetDepreciationScheduleQuery, IReadOnlyList<DepreciationScheduleRow>>
{
    private readonly IAppDbContext _db;
    public GetDepreciationScheduleQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<DepreciationScheduleRow>> Handle(GetDepreciationScheduleQuery request, CancellationToken cancellationToken) =>
        await (
            from e in _db.DepreciationEntries
            join a in _db.FixedAssets on e.FixedAssetId equals a.Id
            orderby e.Year descending, e.Month descending, a.AssetNumber
            select new DepreciationScheduleRow(a.AssetNumber, a.Name, e.Year, e.Month, e.Amount))
            .ToListAsync(cancellationToken);
}

/// <summary>
/// Posts one month's straight-line depreciation for every active asset: a single batched journal
/// Dr Depreciation Expense (5250) / Cr Accumulated Depreciation (1390), per-asset lines. Idempotent on
/// reference DEP-&lt;year&gt;-&lt;month&gt; and on the per-asset (asset, year, month) unique index.
/// </summary>
public record RunDepreciationCommand(int Year, int Month) : IRequest<RunDepreciationResultDto>;

public record RunDepreciationResultDto(int AssetCount, decimal Total, string? JournalEntryNumber, bool AlreadyRun);

public class RunDepreciationCommandValidator : AbstractValidator<RunDepreciationCommand>
{
    public RunDepreciationCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 9999);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

public class RunDepreciationCommandHandler : IRequestHandler<RunDepreciationCommand, RunDepreciationResultDto>
{
    private readonly IAppDbContext _db;
    private readonly IGeneralLedgerService _gl;

    public RunDepreciationCommandHandler(IAppDbContext db, IGeneralLedgerService gl)
    {
        _db = db;
        _gl = gl;
    }

    public async Task<RunDepreciationResultDto> Handle(RunDepreciationCommand request, CancellationToken cancellationToken)
    {
        var reference = $"DEP-{request.Year}-{request.Month:00}";
        if (await _db.JournalEntries.AnyAsync(e => e.Reference == reference && e.Status != JournalStatus.Void, cancellationToken))
            return new RunDepreciationResultDto(0, 0m, null, AlreadyRun: true);

        var assets = await _db.FixedAssets
            .Include(a => a.Entries)
            .Where(a => a.Status == FixedAssetStatus.Active)
            .ToListAsync(cancellationToken);

        // Period date = last day of the month.
        var periodDate = new DateTime(request.Year, request.Month, DateTime.DaysInMonth(request.Year, request.Month));

        var lines = new List<GlPostingLine>();
        var charged = new List<(FixedAsset Asset, decimal Amount)>();
        foreach (var asset in assets)
        {
            if (asset.Entries.Any(e => e.Year == request.Year && e.Month == request.Month)) continue; // already done
            var amount = asset.MonthlyDepreciation();
            if (amount <= 0m) continue;

            lines.Add(GlPostingLine.Dr(GlCodes.DepreciationExpense, amount, $"Depreciation {asset.AssetNumber}"));
            lines.Add(GlPostingLine.Cr(GlCodes.AccumulatedDepreciation, amount, $"Accum. dep. {asset.AssetNumber}"));
            charged.Add((asset, amount));
        }

        if (charged.Count == 0)
            return new RunDepreciationResultDto(0, 0m, null, AlreadyRun: false);

        var entry = await _gl.PostAsync(_db, periodDate, VoucherType.Journal, lines, reference,
            $"Monthly depreciation {reference}", cancellationToken);

        foreach (var (asset, amount) in charged)
            asset.RecordDepreciation(request.Year, request.Month, amount, reference);

        await _db.SaveChangesAsync(cancellationToken);
        return new RunDepreciationResultDto(charged.Count, charged.Sum(c => c.Amount), entry.EntryNumber, AlreadyRun: false);
    }
}
