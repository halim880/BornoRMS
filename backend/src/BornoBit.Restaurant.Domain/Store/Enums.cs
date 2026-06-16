namespace BornoBit.Restaurant.Domain.Store;

/// <summary>Physical dimension a store unit measures. Stock is held in the dimension's base unit (kg / litre / piece).</summary>
public enum StoreUnitDimension
{
    Weight = 1,
    Volume = 2,
    Count = 3
}

/// <summary>Reason a store stock movement (ledger row) was written. Drives the sign of the quantity.</summary>
public enum StoreMovementType
{
    OpeningBalance = 1,
    PurchaseIn = 2,
    IssueOut = 3,
    WastageOut = 4,
    AdjustmentIn = 5,
    AdjustmentOut = 6
}

/// <summary>Lifecycle of a store goods receipt. Only a Posted receipt has moved stock; Voided reverses a posted receipt.</summary>
public enum StoreGoodsReceiptStatus
{
    Draft = 1,
    Posted = 2,
    Voided = 3
}

/// <summary>Lifecycle of a store issue. Only a Posted issue has moved stock out; Voided reverses a posted issue.</summary>
public enum StoreIssueStatus
{
    Draft = 1,
    Posted = 2,
    Voided = 3
}
