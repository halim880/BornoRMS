using BornoBit.Restaurant.Application.Accounting.Posting;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Inventory.Consumption;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Payables;

public record PurchaseReturnLineInput(Guid ItemId, decimal Qty, Guid UnitId);

/// <summary>
/// Return goods to a supplier. Posts immediately: stock is issued out at each item's current moving-average
/// cost (so the average is unchanged — same property as wastage), a <c>PurchaseReturn</c> stock movement is
/// written per line, and the supplier payable is reduced via GL Dr Accounts Payable / Cr Purchases for the
/// total returned value. Reduces the supplier's outstanding balance (see <see cref="GetPayablesQuery"/>).
/// </summary>
public record CreatePurchaseReturnCommand(
    Guid SupplierId,
    DateTime? ReturnedAtUtc,
    string? Reason,
    string? Notes,
    IReadOnlyList<PurchaseReturnLineInput> Lines) : IRequest<Guid>;

public class CreatePurchaseReturnCommandValidator : AbstractValidator<CreatePurchaseReturnCommand>
{
    public CreatePurchaseReturnCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
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

public class CreatePurchaseReturnCommandHandler : IRequestHandler<CreatePurchaseReturnCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly IGeneralLedgerService _gl;
    private readonly TimeProvider _timeProvider;

    public CreatePurchaseReturnCommandHandler(IAppDbContext db, IGeneralLedgerService gl, TimeProvider timeProvider)
    {
        _db = db;
        _gl = gl;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreatePurchaseReturnCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.Suppliers.AnyAsync(s => s.Id == request.SupplierId, cancellationToken))
            throw new NotFoundException($"Supplier {request.SupplierId} not found.");

        var itemIds = request.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await _db.InventoryItems
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        var unitIds = request.Lines.Select(l => l.UnitId).Distinct().ToList();
        var units = await _db.Units.Where(u => unitIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);

        var baseUnitIds = items.Values.Select(i => i.BaseUnitId).Distinct().ToList();
        var baseUnits = await _db.Units.Where(u => baseUnitIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var returnedAt = request.ReturnedAtUtc ?? nowUtc;

        // Pass 1 — resolve and value each line at the item's current average cost (no mutation yet).
        var resolved = new List<(InventoryItem Item, decimal QtyBase, decimal AvgCost, decimal Value)>();
        foreach (var line in request.Lines)
        {
            if (!items.TryGetValue(line.ItemId, out var item))
                throw new NotFoundException($"Stock item {line.ItemId} not found.");
            if (!units.TryGetValue(line.UnitId, out var unit))
                throw new NotFoundException($"Unit {line.UnitId} not found.");
            if (baseUnits.TryGetValue(item.BaseUnitId, out var baseUnit) && baseUnit.Dimension != unit.Dimension)
                throw new ValidationException($"Unit '{unit.Code}' is not compatible with '{item.Name}' (base unit '{baseUnit.Code}').");

            var qtyBase = unit.ToBase(line.Qty);
            if (qtyBase > item.QtyOnHand)
                throw new ValidationException($"Cannot return {line.Qty} {unit.Code} of '{item.Name}' — only {item.QtyOnHand} in stock.");

            resolved.Add((item, qtyBase, item.AvgCost, qtyBase * item.AvgCost));
        }

        var total = resolved.Sum(r => r.Value);
        if (total <= 0m) throw new ValidationException("Returned items have no stock value to credit.");

        var ret = PurchaseReturn.Create(
            await NextReturnNumberAsync(nowUtc, cancellationToken),
            request.SupplierId, returnedAt, total, request.Reason, request.Notes);
        _db.PurchaseReturns.Add(ret);

        // Pass 2 — issue stock out (average cost unchanged) and write the ledger rows.
        foreach (var r in resolved)
        {
            r.Item.WriteOff(r.QtyBase);
            _db.StockMovements.Add(StockMovement.Create(
                r.Item.Id,
                StockMovementType.PurchaseReturn,
                qtyBase: -r.QtyBase,
                occurredAtUtc: returnedAt,
                unitCost: r.AvgCost,
                reason: request.Reason,
                referenceType: nameof(PurchaseReturn),
                referenceId: ret.Id));
            await StockProjectionWriter.BumpAsync(_db, r.Item.Id, r.Item.QtyOnHand, nowUtc, cancellationToken);
        }

        // GL: reverse the receipt accrual for the returned value — Dr Accounts Payable / Cr Purchases.
        var purchasesGl = await SupplierPaymentPoster.ResolvePurchasesGlAsync(_db, cancellationToken);
        await _gl.PostAsync(_db, returnedAt, VoucherType.Journal, new[]
        {
            GlPostingLine.Dr(GlCodes.AccountsPayable, total, $"Purchase return {ret.ReturnNumber}"),
            GlPostingLine.CrId(purchasesGl, total, $"Goods returned to supplier ({ret.ReturnNumber})")
        }, reference: ret.ReturnNumber, narration: $"Purchase return {ret.ReturnNumber}", cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return ret.Id;
    }

    private async Task<string> NextReturnNumberAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var dayStart = nowUtc.Date;
        var dayEnd = dayStart.AddDays(1);
        var countToday = await _db.PurchaseReturns
            .CountAsync(r => r.ReturnedAtUtc >= dayStart && r.ReturnedAtUtc < dayEnd, cancellationToken);
        return $"PRET-{dayStart:yyyyMMdd}-{countToday + 1:D4}";
    }
}
