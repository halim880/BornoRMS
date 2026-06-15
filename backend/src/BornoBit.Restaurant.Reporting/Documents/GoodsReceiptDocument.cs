using BornoBit.Restaurant.Reporting.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BornoBit.Restaurant.Reporting.Documents;

public class GoodsReceiptDocument : IDocument
{
    private readonly GoodsReceiptReportData _data;

    public GoodsReceiptDocument(GoodsReceiptReportData data) { _data = data; }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Goods Receipt {_data.GrnNumber}",
        Author = _data.RestaurantName
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A5);
            page.Margin(28);
            page.DefaultTextStyle(t => t.FontSize(10));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().AlignCenter().Text(t =>
            {
                t.Span("Generated ").FontSize(8).FontColor(Colors.Grey.Medium);
                t.Span($"{_data.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(_data.RestaurantName).FontSize(16).Bold();
                    c.Item().Text("Goods Receipt").FontSize(11).FontColor(Colors.Grey.Darken2);
                });
                row.ConstantItem(170).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text(_data.GrnNumber).Bold();
                    c.Item().AlignRight().Text($"{_data.ReceivedAtUtc:yyyy-MM-dd HH:mm} UTC").FontSize(9).FontColor(Colors.Grey.Darken1);
                    c.Item().AlignRight().Text(_data.Status).FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });

            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Supplier: ").SemiBold();
                    t.Span(_data.SupplierName);
                });
                row.ConstantItem(170).AlignRight().Text(t =>
                {
                    t.Span("Invoice: ").SemiBold();
                    t.Span(string.IsNullOrWhiteSpace(_data.InvoiceNo) ? "— (cash)" : _data.InvoiceNo);
                });
            });

            col.Item().PaddingVertical(6).LineHorizontal(0.7f).LineColor(Colors.Grey.Lighten1);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(4);   // item
                    c.ConstantColumn(70);  // qty
                    c.ConstantColumn(70);  // unit cost
                    c.ConstantColumn(75);  // line total
                });

                table.Header(h =>
                {
                    h.Cell().Text("Item").Bold();
                    h.Cell().AlignRight().Text("Qty").Bold();
                    h.Cell().AlignRight().Text("Unit cost").Bold();
                    h.Cell().AlignRight().Text("Total").Bold();
                });

                foreach (var line in _data.Lines)
                {
                    table.Cell().Text(line.ItemName);
                    table.Cell().AlignRight().Text($"{line.Qty:0.###} {line.UnitCode}");
                    table.Cell().AlignRight().Text(Money(line.UnitCost));
                    table.Cell().AlignRight().Text(Money(line.LineTotal));
                }
            });

            col.Item().PaddingTop(8).AlignRight().Text(t =>
            {
                t.Span("Subtotal: ").SemiBold();
                t.Span($"{_data.Currency} {Money(_data.Subtotal)}").Bold();
            });

            if (!string.IsNullOrWhiteSpace(_data.Notes))
            {
                col.Item().PaddingTop(10).Text(t =>
                {
                    t.Span("Notes: ").SemiBold();
                    t.Span(_data.Notes).FontColor(Colors.Grey.Darken2);
                });
            }
        });
    }

    private static string Money(decimal v) => v.ToString("0.00");
}
