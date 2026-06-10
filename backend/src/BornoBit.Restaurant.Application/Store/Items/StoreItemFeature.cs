using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Store;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Store.Items;

public record StoreItemDto(
    Guid Id,
    string Code,
    string Name,
    string? BanglaName,
    Guid StoreCategoryId,
    string CategoryName,
    Guid BaseUnitId,
    string UnitCode,
    decimal QtyOnHand,
    decimal ReorderLevel,
    decimal ReorderQty,
    decimal AvgCost,
    string Currency,
    bool IsPerishable,
    bool IsActive,
    decimal? PackSize,
    string? PackNote,
    bool IsLowStock,
    decimal StockValue);

// ---- Paged list ----

public record GetStoreItemsQuery(
    string? Search = null,
    Guid? CategoryId = null,
    bool LowStockOnly = false,
    bool IncludeInactive = true,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<StoreItemDto>>;

public class GetStoreItemsQueryHandler : IRequestHandler<GetStoreItemsQuery, PagedResult<StoreItemDto>>
{
    private readonly IAppDbContext _db;
    public GetStoreItemsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<StoreItemDto>> Handle(GetStoreItemsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query =
            from i in _db.StoreItems
            join c in _db.StoreCategories on i.StoreCategoryId equals c.Id
            join u in _db.StoreUnits on i.BaseUnitId equals u.Id
            select new { Item = i, Category = c, Unit = u };

        if (!request.IncludeInactive)
            query = query.Where(x => x.Item.IsActive);

        if (request.CategoryId is { } cid)
            query = query.Where(x => x.Item.StoreCategoryId == cid);

        if (request.LowStockOnly)
            query = query.Where(x => x.Item.ReorderLevel > 0 && x.Item.QtyOnHand <= x.Item.ReorderLevel);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x =>
                EF.Functions.Like(x.Item.Name, $"%{term}%") ||
                EF.Functions.Like(x.Item.Code, $"%{term}%") ||
                (x.Item.BanglaName != null && EF.Functions.Like(x.Item.BanglaName, $"%{term}%")));
        }

        var total = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Category.DisplayOrder).ThenBy(x => x.Item.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new StoreItemDto(
                x.Item.Id, x.Item.Code, x.Item.Name, x.Item.BanglaName,
                x.Item.StoreCategoryId, x.Category.Name,
                x.Item.BaseUnitId, x.Unit.Code,
                x.Item.QtyOnHand, x.Item.ReorderLevel, x.Item.ReorderQty,
                x.Item.AvgCost, x.Item.Currency, x.Item.IsPerishable, x.Item.IsActive,
                x.Item.PackSize, x.Item.PackNote,
                x.Item.ReorderLevel > 0 && x.Item.QtyOnHand <= x.Item.ReorderLevel,
                x.Item.QtyOnHand * x.Item.AvgCost))
            .ToListAsync(cancellationToken);

        return new PagedResult<StoreItemDto>(items, page, pageSize, total);
    }
}

// ---- Low-stock list ----

public record GetLowStockStoreItemsQuery : IRequest<IReadOnlyList<StoreItemDto>>;

public class GetLowStockStoreItemsQueryHandler : IRequestHandler<GetLowStockStoreItemsQuery, IReadOnlyList<StoreItemDto>>
{
    private readonly IAppDbContext _db;
    public GetLowStockStoreItemsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<StoreItemDto>> Handle(GetLowStockStoreItemsQuery request, CancellationToken cancellationToken)
    {
        return await (
            from i in _db.StoreItems
            join c in _db.StoreCategories on i.StoreCategoryId equals c.Id
            join u in _db.StoreUnits on i.BaseUnitId equals u.Id
            where i.IsActive && i.ReorderLevel > 0 && i.QtyOnHand <= i.ReorderLevel
            orderby c.DisplayOrder, i.Name
            select new StoreItemDto(
                i.Id, i.Code, i.Name, i.BanglaName, i.StoreCategoryId, c.Name,
                i.BaseUnitId, u.Code, i.QtyOnHand, i.ReorderLevel, i.ReorderQty,
                i.AvgCost, i.Currency, i.IsPerishable, i.IsActive, i.PackSize, i.PackNote,
                true, i.QtyOnHand * i.AvgCost))
            .ToListAsync(cancellationToken);
    }
}

// ---- Create ----

public record CreateStoreItemCommand(
    string Code,
    string Name,
    string? BanglaName,
    Guid StoreCategoryId,
    Guid BaseUnitId,
    decimal ReorderLevel,
    decimal ReorderQty,
    bool IsPerishable,
    decimal? PackSize,
    string? PackNote
) : IRequest<Guid>;

public class CreateStoreItemCommandValidator : AbstractValidator<CreateStoreItemCommand>
{
    public CreateStoreItemCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BanglaName).MaximumLength(200);
        RuleFor(x => x.StoreCategoryId).NotEmpty();
        RuleFor(x => x.BaseUnitId).NotEmpty();
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderQty).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PackSize).GreaterThan(0).When(x => x.PackSize.HasValue);
        RuleFor(x => x.PackNote).MaximumLength(200);
    }
}

public class CreateStoreItemCommandHandler : IRequestHandler<CreateStoreItemCommand, Guid>
{
    private readonly IAppDbContext _db;
    public CreateStoreItemCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateStoreItemCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.StoreCategories.AnyAsync(c => c.Id == request.StoreCategoryId, cancellationToken))
            throw new NotFoundException($"Store category {request.StoreCategoryId} not found.");
        if (!await _db.StoreUnits.AnyAsync(u => u.Id == request.BaseUnitId, cancellationToken))
            throw new NotFoundException($"Store unit {request.BaseUnitId} not found.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.StoreItems.AnyAsync(i => i.Code == code, cancellationToken))
            throw new ValidationException($"A store item with code '{code}' already exists.");

        var entity = StoreItem.Create(
            code, request.Name, request.StoreCategoryId, request.BaseUnitId,
            request.BanglaName, request.ReorderLevel, request.ReorderQty,
            request.IsPerishable, request.PackSize, request.PackNote);

        _db.StoreItems.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

// ---- Update ----

public record UpdateStoreItemCommand(
    Guid Id,
    string Code,
    string Name,
    string? BanglaName,
    Guid StoreCategoryId,
    Guid BaseUnitId,
    decimal ReorderLevel,
    decimal ReorderQty,
    bool IsPerishable,
    decimal? PackSize,
    string? PackNote
) : IRequest<Unit>;

public class UpdateStoreItemCommandValidator : AbstractValidator<UpdateStoreItemCommand>
{
    public UpdateStoreItemCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BanglaName).MaximumLength(200);
        RuleFor(x => x.StoreCategoryId).NotEmpty();
        RuleFor(x => x.BaseUnitId).NotEmpty();
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderQty).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PackSize).GreaterThan(0).When(x => x.PackSize.HasValue);
        RuleFor(x => x.PackNote).MaximumLength(200);
    }
}

public class UpdateStoreItemCommandHandler : IRequestHandler<UpdateStoreItemCommand, Unit>
{
    private readonly IAppDbContext _db;
    public UpdateStoreItemCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateStoreItemCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.StoreItems.FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Store item {request.Id} not found.");

        if (!await _db.StoreCategories.AnyAsync(c => c.Id == request.StoreCategoryId, cancellationToken))
            throw new NotFoundException($"Store category {request.StoreCategoryId} not found.");
        if (!await _db.StoreUnits.AnyAsync(u => u.Id == request.BaseUnitId, cancellationToken))
            throw new NotFoundException($"Store unit {request.BaseUnitId} not found.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.StoreItems.AnyAsync(i => i.Id != request.Id && i.Code == code, cancellationToken))
            throw new ValidationException($"A store item with code '{code}' already exists.");

        entity.UpdateDetails(
            code, request.Name, request.StoreCategoryId, request.BaseUnitId,
            request.BanglaName, request.ReorderLevel, request.ReorderQty,
            request.IsPerishable, request.PackSize, request.PackNote);

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// ---- SetActive ----

public record SetStoreItemActiveCommand(Guid Id, bool IsActive) : IRequest<Unit>;

public class SetStoreItemActiveCommandValidator : AbstractValidator<SetStoreItemActiveCommand>
{
    public SetStoreItemActiveCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class SetStoreItemActiveCommandHandler : IRequestHandler<SetStoreItemActiveCommand, Unit>
{
    private readonly IAppDbContext _db;
    public SetStoreItemActiveCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(SetStoreItemActiveCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.StoreItems.FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Store item {request.Id} not found.");
        if (request.IsActive) entity.Activate(); else entity.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// ---- Adjust to physical count ----

public record AdjustStoreStockCommand(Guid ItemId, decimal CountedQtyBase, string? Reason) : IRequest<Unit>;

public class AdjustStoreStockCommandValidator : AbstractValidator<AdjustStoreStockCommand>
{
    public AdjustStoreStockCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.CountedQtyBase).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class AdjustStoreStockCommandHandler : IRequestHandler<AdjustStoreStockCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public AdjustStoreStockCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(AdjustStoreStockCommand request, CancellationToken cancellationToken)
    {
        var item = await _db.StoreItems.FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken)
            ?? throw new NotFoundException($"Store item {request.ItemId} not found.");

        var delta = item.AdjustTo(request.CountedQtyBase);
        if (delta == 0) return Unit.Value;

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        _db.StoreStockMovements.Add(StoreStockMovement.Create(
            item.Id,
            delta > 0 ? StoreMovementType.AdjustmentIn : StoreMovementType.AdjustmentOut,
            qtyBase: delta,
            occurredAtUtc: nowUtc,
            unitCost: item.AvgCost,
            reason: request.Reason ?? "Physical count adjustment"));

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
