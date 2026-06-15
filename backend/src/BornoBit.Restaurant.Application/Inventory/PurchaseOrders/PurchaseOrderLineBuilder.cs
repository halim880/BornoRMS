using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.PurchaseOrders;

/// <summary>Resolves items/units, validates unit-dimension compatibility, and pushes lines onto a PO — shared by create + update.</summary>
internal static class PurchaseOrderLineBuilder
{
    public static async Task ApplyLinesAsync(
        IAppDbContext db, PurchaseOrder po, IReadOnlyList<PurchaseOrderLineInput> lines, CancellationToken cancellationToken)
    {
        var itemIds = lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await db.InventoryItems
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        var unitIds = lines.Select(l => l.UnitId).Distinct().ToList();
        var units = await db.Units
            .Where(u => unitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var baseUnitIds = items.Values.Select(i => i.BaseUnitId).Distinct().ToList();
        var baseUnits = await db.Units
            .Where(u => baseUnitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        foreach (var line in lines)
        {
            if (!items.TryGetValue(line.ItemId, out var item))
                throw new NotFoundException($"Stock item {line.ItemId} not found.");
            if (!units.TryGetValue(line.UnitId, out var unit))
                throw new NotFoundException($"Unit {line.UnitId} not found.");

            if (baseUnits.TryGetValue(item.BaseUnitId, out var baseUnit) && baseUnit.Dimension != unit.Dimension)
                throw new ValidationException($"Unit '{unit.Code}' is not compatible with '{item.Name}' (base unit '{baseUnit.Code}').");

            var qtyBase = unit.ToBase(line.Qty);
            po.AddLine(item.Id, item.Name, line.Qty, unit.Id, qtyBase, line.UnitCost);
        }
    }
}
