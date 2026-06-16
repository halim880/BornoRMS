using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Store;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Store.Requisitions;

// ---- DTOs ----

public record StoreRequisitionListItemDto(
    Guid Id,
    string RequisitionNumber,
    Guid StoreDepartmentId,
    string DepartmentName,
    DateTime RequestedAtUtc,
    DateTime? RequiredByUtc,
    StoreRequisitionStatus Status,
    int LineCount);

public record StoreRequisitionLineDto(
    Guid Id,
    Guid StoreItemId,
    string ItemName,
    decimal RequestedQty,
    Guid UnitId,
    string UnitCode,
    decimal RequestedQtyBase,
    decimal ApprovedQtyBase,
    decimal IssuedQtyBase,
    decimal OutstandingQtyBase,
    string BaseUnitCode);

public record StoreRequisitionDetailDto(
    Guid Id,
    string RequisitionNumber,
    Guid StoreDepartmentId,
    string DepartmentName,
    DateTime RequestedAtUtc,
    DateTime? RequiredByUtc,
    string? Notes,
    StoreRequisitionStatus Status,
    DateTime? ApprovedAtUtc,
    string? RejectedReason,
    IReadOnlyList<StoreRequisitionLineDto> Lines);

// ---- List ----

public record GetStoreRequisitionsQuery(
    StoreRequisitionStatus? Status = null,
    Guid? StoreDepartmentId = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<StoreRequisitionListItemDto>>;

public class GetStoreRequisitionsQueryHandler : IRequestHandler<GetStoreRequisitionsQuery, PagedResult<StoreRequisitionListItemDto>>
{
    private readonly IAppDbContext _db;
    public GetStoreRequisitionsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<StoreRequisitionListItemDto>> Handle(GetStoreRequisitionsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = _db.StoreRequisitions.AsQueryable();
        if (request.Status is { } st) query = query.Where(r => r.Status == st);
        if (request.StoreDepartmentId is { } dep) query = query.Where(r => r.StoreDepartmentId == dep);

        var total = await query.LongCountAsync(cancellationToken);

        var items = await (
            from r in query
            join d in _db.StoreDepartments on r.StoreDepartmentId equals d.Id
            orderby r.RequestedAtUtc descending
            select new StoreRequisitionListItemDto(
                r.Id, r.RequisitionNumber, r.StoreDepartmentId, d.Name,
                r.RequestedAtUtc, r.RequiredByUtc, r.Status, r.Lines.Count()))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<StoreRequisitionListItemDto>(items, page, pageSize, total);
    }
}

// ---- Detail ----

public record GetStoreRequisitionQuery(Guid Id) : IRequest<StoreRequisitionDetailDto>;

public class GetStoreRequisitionQueryHandler : IRequestHandler<GetStoreRequisitionQuery, StoreRequisitionDetailDto>
{
    private readonly IAppDbContext _db;
    public GetStoreRequisitionQueryHandler(IAppDbContext db) => _db = db;

    public async Task<StoreRequisitionDetailDto> Handle(GetStoreRequisitionQuery request, CancellationToken cancellationToken)
    {
        var req = await _db.StoreRequisitions.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Store requisition {request.Id} not found.");

        var department = await _db.StoreDepartments
            .Where(d => d.Id == req.StoreDepartmentId)
            .Select(d => d.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? "—";

        var lines = await (
            from l in _db.StoreRequisitionLines
            join u in _db.StoreUnits on l.UnitId equals u.Id
            join i in _db.StoreItems on l.StoreItemId equals i.Id
            join bu in _db.StoreUnits on i.BaseUnitId equals bu.Id
            where l.StoreRequisitionId == req.Id
            select new StoreRequisitionLineDto(
                l.Id, l.StoreItemId, l.ItemName, l.RequestedQty, l.UnitId, u.Code,
                l.RequestedQtyBase, l.ApprovedQtyBase, l.IssuedQtyBase,
                l.ApprovedQtyBase - l.IssuedQtyBase, bu.Code))
            .ToListAsync(cancellationToken);

        return new StoreRequisitionDetailDto(
            req.Id, req.RequisitionNumber, req.StoreDepartmentId, department,
            req.RequestedAtUtc, req.RequiredByUtc, req.Notes, req.Status,
            req.ApprovedAtUtc, req.RejectedReason, lines);
    }
}

// ---- Create draft ----

public record StoreRequisitionLineInput(Guid ItemId, decimal Qty, Guid UnitId);

public record CreateStoreRequisitionCommand(
    Guid StoreDepartmentId,
    DateTime? RequiredByUtc,
    string? Notes,
    IReadOnlyList<StoreRequisitionLineInput> Lines
) : IRequest<Guid>;

public class CreateStoreRequisitionCommandValidator : AbstractValidator<CreateStoreRequisitionCommand>
{
    public CreateStoreRequisitionCommandValidator()
    {
        RuleFor(x => x.StoreDepartmentId).NotEmpty();
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

public class CreateStoreRequisitionCommandHandler : IRequestHandler<CreateStoreRequisitionCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public CreateStoreRequisitionCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreateStoreRequisitionCommand request, CancellationToken cancellationToken)
    {
        await EnsureDepartmentActiveAsync(_db, request.StoreDepartmentId, cancellationToken);

        var ctx = await StoreRequisitionLineResolver.LoadAsync(_db, request.Lines, cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var req = StoreRequisition.Create(
            await NextRequisitionNumberAsync(_db, nowUtc, cancellationToken),
            request.StoreDepartmentId, nowUtc, request.RequiredByUtc, request.Notes);

        foreach (var line in request.Lines)
            ctx.AddTo(req, line);

        _db.StoreRequisitions.Add(req);
        await _db.SaveChangesAsync(cancellationToken);
        return req.Id;
    }

    internal static async Task EnsureDepartmentActiveAsync(IAppDbContext db, Guid departmentId, CancellationToken ct)
    {
        var dep = await db.StoreDepartments.FirstOrDefaultAsync(d => d.Id == departmentId, ct)
            ?? throw new NotFoundException($"Store department {departmentId} not found.");
        if (!dep.IsActive) throw new ValidationException($"Department '{dep.Name}' is inactive.");
    }

    internal static async Task<string> NextRequisitionNumberAsync(IAppDbContext db, DateTime nowUtc, CancellationToken ct)
    {
        var dayStart = nowUtc.Date;
        var dayEnd = dayStart.AddDays(1);
        var countToday = await db.StoreRequisitions
            .CountAsync(r => r.RequestedAtUtc >= dayStart && r.RequestedAtUtc < dayEnd, ct);
        return $"REQ-{dayStart:yyyyMMdd}-{countToday + 1:D4}";
    }
}

// ---- Update draft ----

public record UpdateStoreRequisitionCommand(
    Guid Id,
    Guid StoreDepartmentId,
    DateTime? RequiredByUtc,
    string? Notes,
    IReadOnlyList<StoreRequisitionLineInput> Lines
) : IRequest<Unit>;

public class UpdateStoreRequisitionCommandValidator : AbstractValidator<UpdateStoreRequisitionCommand>
{
    public UpdateStoreRequisitionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.StoreDepartmentId).NotEmpty();
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

public class UpdateStoreRequisitionCommandHandler : IRequestHandler<UpdateStoreRequisitionCommand, Unit>
{
    private readonly IAppDbContext _db;
    public UpdateStoreRequisitionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateStoreRequisitionCommand request, CancellationToken cancellationToken)
    {
        var req = await _db.StoreRequisitions.Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Store requisition {request.Id} not found.");

        if (req.Status != StoreRequisitionStatus.Draft)
            throw new ValidationException($"Only a draft requisition can be edited; '{req.RequisitionNumber}' is {req.Status}.");

        await CreateStoreRequisitionCommandHandler.EnsureDepartmentActiveAsync(_db, request.StoreDepartmentId, cancellationToken);

        var ctx = await StoreRequisitionLineResolver.LoadAsync(_db, request.Lines, cancellationToken);

        req.UpdateHeader(request.StoreDepartmentId, request.RequiredByUtc, request.Notes);
        req.ClearLines();

        foreach (var line in request.Lines)
            ctx.AddTo(req, line);

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// ---- Submit / Approve / Reject / Cancel ----

public record SubmitStoreRequisitionCommand(Guid Id) : IRequest<Unit>;

public class SubmitStoreRequisitionCommandHandler : IRequestHandler<SubmitStoreRequisitionCommand, Unit>
{
    private readonly IAppDbContext _db;
    public SubmitStoreRequisitionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(SubmitStoreRequisitionCommand request, CancellationToken cancellationToken)
    {
        var req = await _db.StoreRequisitions.Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Store requisition {request.Id} not found.");
        req.Submit();
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public record ApproveStoreRequisitionCommand(Guid Id, IReadOnlyDictionary<Guid, decimal>? ApprovedQtyBaseByLineId) : IRequest<Unit>;

public class ApproveStoreRequisitionCommandHandler : IRequestHandler<ApproveStoreRequisitionCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public ApproveStoreRequisitionCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(ApproveStoreRequisitionCommand request, CancellationToken cancellationToken)
    {
        var req = await _db.StoreRequisitions.Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Store requisition {request.Id} not found.");

        req.Approve(
            request.ApprovedQtyBaseByLineId ?? new Dictionary<Guid, decimal>(),
            _timeProvider.GetUtcNow().UtcDateTime);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public record RejectStoreRequisitionCommand(Guid Id, string Reason) : IRequest<Unit>;

public class RejectStoreRequisitionCommandValidator : AbstractValidator<RejectStoreRequisitionCommand>
{
    public RejectStoreRequisitionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class RejectStoreRequisitionCommandHandler : IRequestHandler<RejectStoreRequisitionCommand, Unit>
{
    private readonly IAppDbContext _db;
    public RejectStoreRequisitionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(RejectStoreRequisitionCommand request, CancellationToken cancellationToken)
    {
        var req = await _db.StoreRequisitions.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Store requisition {request.Id} not found.");
        req.Reject(request.Reason);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public record CancelStoreRequisitionCommand(Guid Id) : IRequest<Unit>;

public class CancelStoreRequisitionCommandHandler : IRequestHandler<CancelStoreRequisitionCommand, Unit>
{
    private readonly IAppDbContext _db;
    public CancelStoreRequisitionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(CancelStoreRequisitionCommand request, CancellationToken cancellationToken)
    {
        var req = await _db.StoreRequisitions.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Store requisition {request.Id} not found.");
        req.Cancel();
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// ---- Shared line resolution (items, units, dimension compatibility) ----

internal sealed class StoreRequisitionLineResolver
{
    private readonly IReadOnlyDictionary<Guid, StoreItem> _items;
    private readonly IReadOnlyDictionary<Guid, StoreUnit> _units;
    private readonly IReadOnlyDictionary<Guid, StoreUnit> _baseUnits;

    private StoreRequisitionLineResolver(
        IReadOnlyDictionary<Guid, StoreItem> items,
        IReadOnlyDictionary<Guid, StoreUnit> units,
        IReadOnlyDictionary<Guid, StoreUnit> baseUnits)
    {
        _items = items;
        _units = units;
        _baseUnits = baseUnits;
    }

    public static async Task<StoreRequisitionLineResolver> LoadAsync(
        IAppDbContext db, IReadOnlyList<StoreRequisitionLineInput> lines, CancellationToken ct)
    {
        var itemIds = lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await db.StoreItems.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);

        var unitIds = lines.Select(l => l.UnitId).Distinct().ToList();
        var units = await db.StoreUnits.Where(u => unitIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);

        var baseUnitIds = items.Values.Select(i => i.BaseUnitId).Distinct().ToList();
        var baseUnits = await db.StoreUnits.Where(u => baseUnitIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);

        return new StoreRequisitionLineResolver(items, units, baseUnits);
    }

    public void AddTo(StoreRequisition req, StoreRequisitionLineInput line)
    {
        if (!_items.TryGetValue(line.ItemId, out var item))
            throw new NotFoundException($"Store item {line.ItemId} not found.");
        if (!_units.TryGetValue(line.UnitId, out var unit))
            throw new NotFoundException($"Store unit {line.UnitId} not found.");

        if (_baseUnits.TryGetValue(item.BaseUnitId, out var baseUnit) && baseUnit.Dimension != unit.Dimension)
            throw new ValidationException($"Unit '{unit.Code}' is not compatible with '{item.Name}' (base unit '{baseUnit.Code}').");

        var qtyBase = unit.ToBase(line.Qty);
        req.AddLine(item.Id, item.Name, line.Qty, unit.Id, qtyBase);
    }
}
