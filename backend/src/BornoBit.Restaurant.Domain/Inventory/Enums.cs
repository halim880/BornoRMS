namespace BornoBit.Restaurant.Domain.Inventory;

/// <summary>Physical dimension a unit measures. Stock is held in the dimension's base unit (kg / litre / piece).</summary>
public enum UnitDimension
{
    Weight = 1,
    Volume = 2,
    Count = 3
}

/// <summary>Whether a stock item is a raw ingredient or a finished/packaged good sold as-is.</summary>
public enum InventoryItemType
{
    Ingredient = 1,
    FinishedGood = 2
}

/// <summary>Reason a stock movement (ledger row) was written. Drives the sign of the quantity.</summary>
public enum StockMovementType
{
    OpeningBalance = 1,
    PurchaseIn = 2,
    WastageOut = 3,
    AdjustmentIn = 4,
    AdjustmentOut = 5,
    ConsumptionOut = 6
}

/// <summary>Lifecycle of a goods receipt. Only a Posted receipt has moved stock.</summary>
public enum GoodsReceiptStatus
{
    Draft = 1,
    Posted = 2
}
