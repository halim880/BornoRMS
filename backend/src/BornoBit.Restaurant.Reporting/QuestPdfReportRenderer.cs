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
}
