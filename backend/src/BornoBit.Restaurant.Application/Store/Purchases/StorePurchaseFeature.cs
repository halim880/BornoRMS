using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Store;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Store.Purchases;

// ---- DTOs ----

public record StoreGoodsReceiptListItemDto(
    Guid Id,
    string GrnNumber,
    Guid StoreSupplierId,
    string SupplierName,
    string? InvoiceNo,
    DateTime ReceivedAtUtc,
    string Currency,
    StoreGoodsReceiptStatus Status,
    int LineCount,
    decimal Subtotal);

public record StoreGoodsReceiptLineDto(
    Guid Id,
    Guid StoreItemId,
    string ItemName,
    decimal Qty,
    Guid UnitId,
    string UnitCode,
    decimal QtyBase,
    decimal UnitCost,
    decimal LineTotal);

public record StoreGoodsReceiptDetailDto(
    Guid Id,
    string GrnNumber,
    Guid StoreSupplierId,
    string SupplierName,
    string? InvoiceNo,
    DateTime ReceivedAtUtc,
    string Currency,
    string? Notes,
    StoreGoodsReceiptStatus Status,
    DateTime? PostedAtUtc,
    decimal Subtotal,
    IReadOnlyList<StoreGoodsReceiptLineDto> Lines);

// ---- List ----

public record GetStoreGoodsReceiptsQuery(StoreGoodsReceiptStatus? Status = null, int Page = 1, int PageSize = 50)
    : IRequest<PagedResult<StoreGoodsReceiptListItemDto>>;

public class GetStoreGoodsReceiptsQueryHandler : IRequestHandler<GetStoreGoodsReceiptsQuery, PagedResult<StoreGoodsReceiptListItemDto>>
{
    private readonly IAppDbContext _db;
    public GetStoreGoodsReceiptsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<StoreGoodsReceiptListItemDto>> Handle(GetStoreGoodsReceiptsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query =
            from g in _db.StoreGoodsReceipts
            join s in _db.StoreSuppliers on g.StoreSupplierId equals s.Id
            select new { Grn = g, Supplier = s };

        if (request.Status is { } st)
            query = query.Where(x => x.Grn.Status == st);

        var total = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Grn.ReceivedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new StoreGoodsReceiptListItemDto(
                x.Grn.Id, x.Grn.GrnNumber, x.Grn.StoreSupplierId, x.Supplier.Name,
                x.Grn.InvoiceNo, x.Grn.ReceivedAtUtc, x.Grn.Currency, x.Grn.Status,
                x.Grn.Lines.Count(),
                x.Grn.Lines.Sum(l => (decimal?)l.Qty * l.UnitCost) ?? 0m))
            .ToListAsync(cancellationToken);

        return new PagedResult<StoreGoodsReceiptListItemDto>(items, page, pageSize, total);
    }
}

// ---- Detail ----

public record GetStoreGoodsReceiptQuery(Guid Id) : IRequest<StoreGoodsReceiptDetailDto>;

public class GetStoreGoodsReceiptQueryHandler : IRequestHandler<GetStoreGoodsReceiptQuery, StoreGoodsReceiptDetailDto>
{
    private readonly IAppDbContext _db;
    public GetStoreGoodsReceiptQueryHandler(IAppDbContext db) => _db = db;

    public async Task<StoreGoodsReceiptDetailDto> Handle(GetStoreGoodsReceiptQuery request, CancellationToken cancellationToken)
    {
        var grn = await _db.StoreGoodsReceipts.FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Store goods receipt {request.Id} not found.");

        var supplierName = await _db.StoreSuppliers
            .Where(s => s.Id == grn.StoreSupplierId)
            .Select(s => s.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? "(unknown)";

        var lines = await (
            from l in _db.StoreGoodsReceiptLines
            join u in _db.StoreUnits on l.UnitId equals u.Id
            where l.StoreGoodsReceiptId == grn.Id
            select new StoreGoodsReceiptLineDto(
                l.Id, l.StoreItemId, l.ItemName, l.Qty, l.UnitId, u.Code, l.QtyBase, l.UnitCost, l.Qty * l.UnitCost))
            .ToListAsync(cancellationToken);

        return new StoreGoodsReceiptDetailDto(
            grn.Id, grn.GrnNumber, grn.StoreSupplierId, supplierName, grn.InvoiceNo, grn.ReceivedAtUtc,
            grn.Currency, grn.Notes, grn.Status, grn.PostedAtUtc, lines.Sum(l => l.LineTotal), lines);
    }
}

// ---- Create draft ----

public record StoreGoodsReceiptLineInput(Guid ItemId, decimal Qty, Guid UnitId, decimal UnitCost);

public record CreateStoreGoodsReceiptCommand(
    Guid SupplierId,
    string? InvoiceNo,
    DateTime? ReceivedAtUtc,
    string? Notes,
    IReadOnlyList<StoreGoodsReceiptLineInput> Lines
) : IRequest<Guid>;

public class CreateStoreGoodsReceiptCommandValidator : AbstractValidator<CreateStoreGoodsReceiptCommand>
{
    public CreateStoreGoodsReceiptCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.InvoiceNo).MaximumLength(80);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one line is required.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).NotEmpty();
            line.RuleFor(l => l.UnitId).NotEmpty();
            line.RuleFor(l => l.Qty).GreaterThan(0);
            line.RuleFor(l => l.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}

public class CreateStoreGoodsReceiptCommandHandler : IRequestHandler<CreateStoreGoodsReceiptCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public CreateStoreGoodsReceiptCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreateStoreGoodsReceiptCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.StoreSuppliers.AnyAsync(s => s.Id == request.SupplierId, cancellationToken))
            throw new NotFoundException($"Store supplier {request.SupplierId} not found.");

        var itemIds = request.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await _db.StoreItems.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);

        var unitIds = request.Lines.Select(l => l.UnitId).Distinct().ToList();
        var units = await _db.StoreUnits.Where(u => unitIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);

        var baseUnitIds = items.Values.Select(i => i.BaseUnitId).Distinct().ToList();
        var baseUnits = await _db.StoreUnits.Where(u => baseUnitIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var receivedAt = request.ReceivedAtUtc ?? nowUtc;

        var grn = StoreGoodsReceipt.Create(
            await NextGrnNumberAsync(nowUtc, cancellationToken),
            request.SupplierId, receivedAt, request.InvoiceNo, notes: request.Notes);

        foreach (var line in request.Lines)
        {
            if (!items.TryGetValue(line.ItemId, out var item))
                throw new NotFoundException($"Store item {line.ItemId} not found.");
            if (!units.TryGetValue(line.UnitId, out var unit))
                throw new NotFoundException($"Store unit {line.UnitId} not found.");

            if (baseUnits.TryGetValue(item.BaseUnitId, out var baseUnit) && baseUnit.Dimension != unit.Dimension)
                throw new ValidationException($"Unit '{unit.Code}' is not compatible with '{item.Name}' (base unit '{baseUnit.Code}').");

            var qtyBase = unit.ToBase(line.Qty);
            grn.AddLine(item.Id, item.Name, line.Qty, unit.Id, qtyBase, line.UnitCost);
        }

        _db.StoreGoodsReceipts.Add(grn);
        await _db.SaveChangesAsync(cancellationToken);
        return grn.Id;
    }

    private async Task<string> NextGrnNumberAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var dayStart = nowUtc.Date;
        var dayEnd = dayStart.AddDays(1);
        var countToday = await _db.StoreGoodsReceipts
            .CountAsync(g => g.ReceivedAtUtc >= dayStart && g.ReceivedAtUtc < dayEnd, cancellationToken);
        return $"SGRN-{dayStart:yyyyMMdd}-{countToday + 1:D4}";
    }
}

// ---- Post ----

public record PostStoreGoodsReceiptCommand(Guid Id) : IRequest<Unit>;

public class PostStoreGoodsReceiptCommandValidator : AbstractValidator<PostStoreGoodsReceiptCommand>
{
    public PostStoreGoodsReceiptCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class PostStoreGoodsReceiptCommandHandler : IRequestHandler<PostStoreGoodsReceiptCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public PostStoreGoodsReceiptCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(PostStoreGoodsReceiptCommand request, CancellationToken cancellationToken)
    {
        var grn = await _db.StoreGoodsReceipts.Include(g => g.Lines)
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Store goods receipt {request.Id} not found.");

        if (grn.Status == StoreGoodsReceiptStatus.Posted)
            throw new ValidationException($"Store goods receipt '{grn.GrnNumber}' is already posted.");

        var itemIds = grn.Lines.Select(l => l.StoreItemId).Distinct().ToList();
        var items = await _db.StoreItems.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);

        foreach (var line in grn.Lines)
        {
            if (!items.TryGetValue(line.StoreItemId, out var item))
                throw new NotFoundException($"Store item {line.StoreItemId} not found.");

            item.Receive(line.QtyBase, line.UnitCost);

            _db.StoreStockMovements.Add(StoreStockMovement.Create(
                item.Id, StoreMovementType.PurchaseIn,
                qtyBase: line.QtyBase, occurredAtUtc: grn.ReceivedAtUtc,
                unitCost: line.UnitCost,
                referenceType: nameof(StoreGoodsReceipt), referenceId: grn.Id));
        }

        grn.MarkPosted(_timeProvider.GetUtcNow().UtcDateTime);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
