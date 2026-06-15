using BornoBit.Restaurant.Domain.Inventory;

namespace BornoBit.Restaurant.Application.Inventory.PurchaseOrders;

public record PurchaseOrderListItemDto(
    Guid Id,
    string PoNumber,
    Guid SupplierId,
    string SupplierName,
    DateTime OrderedAtUtc,
    DateTime? ExpectedAtUtc,
    string Currency,
    PurchaseOrderStatus Status,
    int LineCount,
    decimal Subtotal);

public record PurchaseOrderLineDto(
    Guid Id,
    Guid InventoryItemId,
    string ItemName,
    decimal QtyOrdered,
    Guid UnitId,
    string UnitCode,
    decimal QtyOrderedBase,
    decimal QtyReceivedBase,
    decimal OutstandingBase,
    decimal UnitCost,
    decimal LineTotal);

public record PurchaseOrderDetailDto(
    Guid Id,
    string PoNumber,
    Guid SupplierId,
    string SupplierName,
    DateTime OrderedAtUtc,
    DateTime? ExpectedAtUtc,
    string Currency,
    string? Notes,
    PurchaseOrderStatus Status,
    DateTime? ApprovedAtUtc,
    decimal Subtotal,
    IReadOnlyList<PurchaseOrderLineDto> Lines,
    IReadOnlyList<PurchaseOrderReceiptDto> Receipts);

/// <summary>A goods receipt raised against this PO — shown on the matching view.</summary>
public record PurchaseOrderReceiptDto(
    Guid Id,
    string GrnNumber,
    DateTime ReceivedAtUtc,
    GoodsReceiptStatus Status,
    decimal Subtotal);
