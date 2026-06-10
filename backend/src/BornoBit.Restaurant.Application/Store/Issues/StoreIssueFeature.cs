using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Store;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Store.Issues;

// ---- DTOs ----

public record StoreIssueListItemDto(
    Guid Id,
    string IssueNumber,
    string Destination,
    DateTime IssuedAtUtc,
    StoreIssueStatus Status,
    int LineCount,
    decimal TotalQtyBase);

public record StoreIssueLineDto(
    Guid Id,
    Guid StoreItemId,
    string ItemName,
    decimal Qty,
    Guid UnitId,
    string UnitCode,
    decimal QtyBase);

public record StoreIssueDetailDto(
    Guid Id,
    string IssueNumber,
    string Destination,
    DateTime IssuedAtUtc,
    string? Notes,
    StoreIssueStatus Status,
    DateTime? PostedAtUtc,
    IReadOnlyList<StoreIssueLineDto> Lines);

// ---- List ----

public record GetStoreIssuesQuery(StoreIssueStatus? Status = null, int Page = 1, int PageSize = 50)
    : IRequest<PagedResult<StoreIssueListItemDto>>;

public class GetStoreIssuesQueryHandler : IRequestHandler<GetStoreIssuesQuery, PagedResult<StoreIssueListItemDto>>
{
    private readonly IAppDbContext _db;
    public GetStoreIssuesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<StoreIssueListItemDto>> Handle(GetStoreIssuesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = _db.StoreIssues.AsQueryable();
        if (request.Status is { } st) query = query.Where(g => g.Status == st);

        var total = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(g => g.IssuedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new StoreIssueListItemDto(
                g.Id, g.IssueNumber, g.Destination, g.IssuedAtUtc, g.Status,
                g.Lines.Count(),
                g.Lines.Sum(l => (decimal?)l.QtyBase) ?? 0m))
            .ToListAsync(cancellationToken);

        return new PagedResult<StoreIssueListItemDto>(items, page, pageSize, total);
    }
}

// ---- Detail ----

public record GetStoreIssueQuery(Guid Id) : IRequest<StoreIssueDetailDto>;

public class GetStoreIssueQueryHandler : IRequestHandler<GetStoreIssueQuery, StoreIssueDetailDto>
{
    private readonly IAppDbContext _db;
    public GetStoreIssueQueryHandler(IAppDbContext db) => _db = db;

    public async Task<StoreIssueDetailDto> Handle(GetStoreIssueQuery request, CancellationToken cancellationToken)
    {
        var issue = await _db.StoreIssues.FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Store issue {request.Id} not found.");

        var lines = await (
            from l in _db.StoreIssueLines
            join u in _db.StoreUnits on l.UnitId equals u.Id
            where l.StoreIssueId == issue.Id
            select new StoreIssueLineDto(l.Id, l.StoreItemId, l.ItemName, l.Qty, l.UnitId, u.Code, l.QtyBase))
            .ToListAsync(cancellationToken);

        return new StoreIssueDetailDto(
            issue.Id, issue.IssueNumber, issue.Destination, issue.IssuedAtUtc,
            issue.Notes, issue.Status, issue.PostedAtUtc, lines);
    }
}

// ---- Create draft ----

public record StoreIssueLineInput(Guid ItemId, decimal Qty, Guid UnitId);

public record CreateStoreIssueCommand(
    string Destination,
    DateTime? IssuedAtUtc,
    string? Notes,
    IReadOnlyList<StoreIssueLineInput> Lines
) : IRequest<Guid>;

public class CreateStoreIssueCommandValidator : AbstractValidator<CreateStoreIssueCommand>
{
    public CreateStoreIssueCommandValidator()
    {
        RuleFor(x => x.Destination).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one line is required.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).NotEmpty();
            line.RuleFor(l => l.UnitId).NotEmpty();
            line.RuleFor(l => l.Qty).GreaterThan(0);
        });
    }
}

public class CreateStoreIssueCommandHandler : IRequestHandler<CreateStoreIssueCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public CreateStoreIssueCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreateStoreIssueCommand request, CancellationToken cancellationToken)
    {
        var itemIds = request.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await _db.StoreItems.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);

        var unitIds = request.Lines.Select(l => l.UnitId).Distinct().ToList();
        var units = await _db.StoreUnits.Where(u => unitIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);

        var baseUnitIds = items.Values.Select(i => i.BaseUnitId).Distinct().ToList();
        var baseUnits = await _db.StoreUnits.Where(u => baseUnitIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var issuedAt = request.IssuedAtUtc ?? nowUtc;

        var issue = StoreIssue.Create(
            await NextIssueNumberAsync(nowUtc, cancellationToken),
            request.Destination, issuedAt, request.Notes);

        foreach (var line in request.Lines)
        {
            if (!items.TryGetValue(line.ItemId, out var item))
                throw new NotFoundException($"Store item {line.ItemId} not found.");
            if (!units.TryGetValue(line.UnitId, out var unit))
                throw new NotFoundException($"Store unit {line.UnitId} not found.");

            if (baseUnits.TryGetValue(item.BaseUnitId, out var baseUnit) && baseUnit.Dimension != unit.Dimension)
                throw new ValidationException($"Unit '{unit.Code}' is not compatible with '{item.Name}' (base unit '{baseUnit.Code}').");

            var qtyBase = unit.ToBase(line.Qty);
            issue.AddLine(item.Id, item.Name, line.Qty, unit.Id, qtyBase);
        }

        _db.StoreIssues.Add(issue);
        await _db.SaveChangesAsync(cancellationToken);
        return issue.Id;
    }

    private async Task<string> NextIssueNumberAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var dayStart = nowUtc.Date;
        var dayEnd = dayStart.AddDays(1);
        var countToday = await _db.StoreIssues
            .CountAsync(g => g.IssuedAtUtc >= dayStart && g.IssuedAtUtc < dayEnd, cancellationToken);
        return $"ISS-{dayStart:yyyyMMdd}-{countToday + 1:D4}";
    }
}

// ---- Post (decrement stock) ----

public record PostStoreIssueCommand(Guid Id) : IRequest<Unit>;

public class PostStoreIssueCommandValidator : AbstractValidator<PostStoreIssueCommand>
{
    public PostStoreIssueCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class PostStoreIssueCommandHandler : IRequestHandler<PostStoreIssueCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public PostStoreIssueCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(PostStoreIssueCommand request, CancellationToken cancellationToken)
    {
        var issue = await _db.StoreIssues.Include(g => g.Lines)
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Store issue {request.Id} not found.");

        if (issue.Status == StoreIssueStatus.Posted)
            throw new ValidationException($"Store issue '{issue.IssueNumber}' is already posted.");

        var itemIds = issue.Lines.Select(l => l.StoreItemId).Distinct().ToList();
        var items = await _db.StoreItems.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);

        // Pre-check stock so the whole issue fails cleanly before any mutation.
        foreach (var line in issue.Lines)
        {
            if (!items.TryGetValue(line.StoreItemId, out var item))
                throw new NotFoundException($"Store item {line.StoreItemId} not found.");
            if (line.QtyBase > item.QtyOnHand)
                throw new ValidationException($"Cannot issue {line.QtyBase} of '{item.Name}'; only {item.QtyOnHand} on hand.");
        }

        foreach (var line in issue.Lines)
        {
            var item = items[line.StoreItemId];
            item.Issue(line.QtyBase);

            _db.StoreStockMovements.Add(StoreStockMovement.Create(
                item.Id, StoreMovementType.IssueOut,
                qtyBase: -line.QtyBase, occurredAtUtc: issue.IssuedAtUtc,
                unitCost: item.AvgCost,
                reason: $"Issued to {issue.Destination}",
                referenceType: nameof(StoreIssue), referenceId: issue.Id));
        }

        issue.MarkPosted(_timeProvider.GetUtcNow().UtcDateTime);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
