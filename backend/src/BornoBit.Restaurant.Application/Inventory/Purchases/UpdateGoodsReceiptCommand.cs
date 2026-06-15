using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Purchases;

/// <summary>Replace the header and all lines of a Draft goods receipt. Posted receipts are immutable.</summary>
public record UpdateGoodsReceiptCommand(
    Guid Id,
    Guid SupplierId,
    string? InvoiceNo,
    DateTime? ReceivedAtUtc,
    string? Notes,
    IReadOnlyList<GoodsReceiptLineInput> Lines
) : IRequest<Unit>;

public class UpdateGoodsReceiptCommandValidator : AbstractValidator<UpdateGoodsReceiptCommand>
{
    public UpdateGoodsReceiptCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
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

public class UpdateGoodsReceiptCommandHandler : IRequestHandler<UpdateGoodsReceiptCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public UpdateGoodsReceiptCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(UpdateGoodsReceiptCommand request, CancellationToken cancellationToken)
    {
        var grn = await _db.GoodsReceipts
            .Include(g => g.Lines)
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Goods receipt {request.Id} not found.");

        if (grn.Status != Domain.Inventory.GoodsReceiptStatus.Draft)
            throw new ValidationException("Only draft receipts can be edited.");

        if (!await _db.Suppliers.AnyAsync(s => s.Id == request.SupplierId, cancellationToken))
            throw new NotFoundException($"Supplier {request.SupplierId} not found.");

        var itemIds = request.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await _db.InventoryItems
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        var unitIds = request.Lines.Select(l => l.UnitId).Distinct().ToList();
        var units = await _db.Units
            .Where(u => unitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var baseUnitIds = items.Values.Select(i => i.BaseUnitId).Distinct().ToList();
        var baseUnits = await _db.Units
            .Where(u => baseUnitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        grn.UpdateHeader(request.SupplierId, request.ReceivedAtUtc ?? nowUtc, request.InvoiceNo, request.Notes);
        grn.ClearLines();

        foreach (var line in request.Lines)
        {
            if (!items.TryGetValue(line.ItemId, out var item))
                throw new NotFoundException($"Stock item {line.ItemId} not found.");
            if (!units.TryGetValue(line.UnitId, out var unit))
                throw new NotFoundException($"Unit {line.UnitId} not found.");

            if (baseUnits.TryGetValue(item.BaseUnitId, out var baseUnit) && baseUnit.Dimension != unit.Dimension)
                throw new ValidationException($"Unit '{unit.Code}' is not compatible with '{item.Name}' (base unit '{baseUnit.Code}').");

            var qtyBase = unit.ToBase(line.Qty);
            grn.AddLine(item.Id, item.Name, line.Qty, unit.Id, qtyBase, line.UnitCost);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
