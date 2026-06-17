using BornoBit.Restaurant.Reporting.Documents;
using BornoBit.Restaurant.Reporting.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace BornoBit.Restaurant.Reporting;

public class QuestPdfReportRenderer : IReportRenderer
{
    static QuestPdfReportRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> RenderOrderReceiptAsync(OrderReceiptReportData data, CancellationToken cancellationToken = default)
    {
        var doc = new OrderReceiptDocument(data);
        return Task.FromResult(doc.GeneratePdf());
    }

    public Task<byte[]> RenderPosReceiptAsync(OrderReceiptReportData data, CancellationToken cancellationToken = default)
    {
        var doc = new PosReceiptDocument(data);
        return Task.FromResult(doc.GeneratePdf());
    }

    public Task<byte[]> RenderKitchenTicketAsync(KitchenTicketReportData data, CancellationToken cancellationToken = default)
    {
        var doc = new KitchenTicketDocument(data);
        return Task.FromResult(doc.GeneratePdf());
    }

    public Task<byte[]> RenderStockValuationAsync(StockValuationReportData data, CancellationToken cancellationToken = default)
    {
        var doc = new StockValuationDocument(data);
        return Task.FromResult(doc.GeneratePdf());
    }

    public Task<byte[]> RenderGoodsReceiptAsync(GoodsReceiptReportData data, CancellationToken cancellationToken = default)
    {
        var doc = new GoodsReceiptDocument(data);
        return Task.FromResult(doc.GeneratePdf());
    }

    public Task<byte[]> RenderStoreIssueVoucherAsync(StoreIssueVoucherReportData data, CancellationToken cancellationToken = default)
    {
        var doc = new StoreIssueVoucherDocument(data);
        return Task.FromResult(doc.GeneratePdf());
    }

    public Task<byte[]> RenderStoreMovementLedgerAsync(StoreMovementLedgerReportData data, CancellationToken cancellationToken = default)
    {
        var doc = new StoreMovementLedgerDocument(data);
        return Task.FromResult(doc.GeneratePdf());
    }

    public Task<byte[]> RenderStoreDepartmentConsumptionAsync(StoreDepartmentConsumptionReportData data, CancellationToken cancellationToken = default)
    {
        var doc = new StoreDepartmentConsumptionDocument(data);
        return Task.FromResult(doc.GeneratePdf());
    }

    public Task<byte[]> RenderSalesInvoiceReportAsync(SalesInvoiceReportData data, CancellationToken cancellationToken = default)
    {
        var doc = new SalesInvoiceReportDocument(data);
        return Task.FromResult(doc.GeneratePdf());
    }
}
