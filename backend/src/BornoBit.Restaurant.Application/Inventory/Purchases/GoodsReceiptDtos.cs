using BornoBit.Restaurant.Domain.Inventory;

namespace BornoBit.Restaurant.Application.Inventory.Purchases;

public record GoodsReceiptListItemDto(
    Guid Id,
    string GrnNumber,
    Guid SupplierId,
    string SupplierName,
    string? InvoiceNo,
    DateTime ReceivedAtUtc,
    string Currency,
    GoodsReceiptStatus Status,
    int LineCount,
    decimal Subtotal);

public record GoodsReceiptLineDto(
    Guid Id,
    Guid InventoryItemId,
    string ItemName,
    decimal Qty,
    Guid UnitId,
    string UnitCode,
    decimal QtyBase,
    decimal UnitCost,
    decimal LineTotal);

public record GoodsReceiptDetailDto(
    Guid Id,
    string GrnNumber,
    Guid SupplierId,
    string SupplierName,
    string? InvoiceNo,
    DateTime ReceivedAtUtc,
    string Currency,
    string? Notes,
    GoodsReceiptStatus Status,
    DateTime? PostedAtUtc,
    decimal Subtotal,
    IReadOnlyList<GoodsReceiptLineDto> Lines);
