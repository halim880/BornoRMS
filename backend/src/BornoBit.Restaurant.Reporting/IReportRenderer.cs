using BornoBit.Restaurant.Reporting.Models;

namespace BornoBit.Restaurant.Reporting;

public interface IReportRenderer
{
    Task<byte[]> RenderOrderReceiptAsync(OrderReceiptReportData data, CancellationToken cancellationToken = default);
    Task<byte[]> RenderPosReceiptAsync(OrderReceiptReportData data, CancellationToken cancellationToken = default);
    Task<byte[]> RenderKitchenTicketAsync(KitchenTicketReportData data, CancellationToken cancellationToken = default);
    Task<byte[]> RenderStockValuationAsync(StockValuationReportData data, CancellationToken cancellationToken = default);
    Task<byte[]> RenderGoodsReceiptAsync(GoodsReceiptReportData data, CancellationToken cancellationToken = default);
    Task<byte[]> RenderStoreIssueVoucherAsync(StoreIssueVoucherReportData data, CancellationToken cancellationToken = default);
}
