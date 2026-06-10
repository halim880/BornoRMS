using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Store;

/// <summary>
/// A warehouse/store stock item. <see cref="QtyOnHand"/> and <see cref="AvgCost"/> are caches maintained
/// by the behavior methods; every change also writes a <see cref="StoreStockMovement"/> ledger row at the
/// application layer. Quantities are held in the base unit of <see cref="BaseUnitId"/>'s dimension.
/// Fully isolated from the POS InventoryItem (separate table, no shared rows or foreign keys).
/// </summary>
public class StoreItem : AuditableEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? BanglaName { get; private set; }
    public Guid StoreCategoryId { get; private set; }
    public Guid BaseUnitId { get; private set; }

    public decimal QtyOnHand { get; private set; }
    public decimal ReorderLevel { get; private set; }
    public decimal ReorderQty { get; private set; }
    public decimal AvgCost { get; private set; }
    public string Currency { get; private set; } = "Tk";

    public bool IsPerishable { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>Informational pack size in base units (e.g. 1 bosta = 50 kg); sacks vary by item so this is per-item.</summary>
    public decimal? PackSize { get; private set; }
    public string? PackNote { get; private set; }

    public bool IsLowStock => ReorderLevel > 0 && QtyOnHand <= ReorderLevel;
    public decimal StockValue => QtyOnHand * AvgCost;

    private StoreItem() { }

    public static StoreItem Create(
        string code,
        string name,
        Guid storeCategoryId,
        Guid baseUnitId,
        string? banglaName = null,
        decimal reorderLevel = 0m,
        decimal reorderQty = 0m,
        bool isPerishable = false,
        decimal? packSize = null,
        string? packNote = null,
        string currency = "Tk")
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (storeCategoryId == Guid.Empty) throw new ArgumentException("Category is required.", nameof(storeCategoryId));
        if (baseUnitId == Guid.Empty) throw new ArgumentException("Base unit is required.", nameof(baseUnitId));
        if (reorderLevel < 0) throw new ArgumentOutOfRangeException(nameof(reorderLevel));
        if (reorderQty < 0) throw new ArgumentOutOfRangeException(nameof(reorderQty));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));

        return new StoreItem
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            BanglaName = Trim(banglaName),
            StoreCategoryId = storeCategoryId,
            BaseUnitId = baseUnitId,
            ReorderLevel = reorderLevel,
            ReorderQty = reorderQty,
            IsPerishable = isPerishable,
            PackSize = packSize,
            PackNote = Trim(packNote),
            Currency = currency.Trim(),
            IsActive = true
        };
    }

    public void UpdateDetails(
        string code,
        string name,
        Guid storeCategoryId,
        Guid baseUnitId,
        string? banglaName,
        decimal reorderLevel,
        decimal reorderQty,
        bool isPerishable,
        decimal? packSize,
        string? packNote)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (storeCategoryId == Guid.Empty) throw new ArgumentException("Category is required.", nameof(storeCategoryId));
        if (baseUnitId == Guid.Empty) throw new ArgumentException("Base unit is required.", nameof(baseUnitId));
        if (reorderLevel < 0) throw new ArgumentOutOfRangeException(nameof(reorderLevel));
        if (reorderQty < 0) throw new ArgumentOutOfRangeException(nameof(reorderQty));

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        StoreCategoryId = storeCategoryId;
        BaseUnitId = baseUnitId;
        BanglaName = Trim(banglaName);
        ReorderLevel = reorderLevel;
        ReorderQty = reorderQty;
        IsPerishable = isPerishable;
        PackSize = packSize;
        PackNote = Trim(packNote);
    }

    /// <summary>Receive stock (purchase / opening balance). Recomputes the moving-average unit cost.</summary>
    public void Receive(decimal qtyBase, decimal unitCost)
    {
        if (qtyBase <= 0) throw new ArgumentOutOfRangeException(nameof(qtyBase), "Received quantity must be positive.");
        if (unitCost < 0) throw new ArgumentOutOfRangeException(nameof(unitCost), "Unit cost cannot be negative.");

        var newQty = QtyOnHand + qtyBase;
        AvgCost = newQty == 0 ? 0m : ((QtyOnHand * AvgCost) + (qtyBase * unitCost)) / newQty;
        QtyOnHand = newQty;
    }

    /// <summary>Issue stock out to a destination (kitchen / department / branch).</summary>
    public void Issue(decimal qtyBase)
    {
        if (qtyBase <= 0) throw new ArgumentOutOfRangeException(nameof(qtyBase));
        if (qtyBase > QtyOnHand) throw new InvalidOperationException($"Cannot issue {qtyBase}; only {QtyOnHand} on hand for '{Name}'.");
        QtyOnHand -= qtyBase;
    }

    /// <summary>Reduce stock through wastage / spoilage (nosto).</summary>
    public void WriteOff(decimal qtyBase)
    {
        if (qtyBase <= 0) throw new ArgumentOutOfRangeException(nameof(qtyBase));
        if (qtyBase > QtyOnHand) throw new InvalidOperationException($"Cannot write off {qtyBase}; only {QtyOnHand} on hand for '{Name}'.");
        QtyOnHand -= qtyBase;
    }

    /// <summary>Reconcile to a physical count. Returns the signed delta (counted − previous).</summary>
    public decimal AdjustTo(decimal countedBase)
    {
        if (countedBase < 0) throw new ArgumentOutOfRangeException(nameof(countedBase), "Counted quantity cannot be negative.");
        var delta = countedBase - QtyOnHand;
        QtyOnHand = countedBase;
        return delta;
    }

    public void SetReorder(decimal reorderLevel, decimal reorderQty)
    {
        if (reorderLevel < 0) throw new ArgumentOutOfRangeException(nameof(reorderLevel));
        if (reorderQty < 0) throw new ArgumentOutOfRangeException(nameof(reorderQty));
        ReorderLevel = reorderLevel;
        ReorderQty = reorderQty;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
