using BornoBit.Restaurant.Reporting.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BornoBit.Restaurant.Reporting.Documents;

public class StockValuationDocument : IDocument
{
    private readonly StockValuationReportData _data;

    public StockValuationDocument(StockValuationReportData data) { _data = data; }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = "Stock Valuation",
        Author = _data.RestaurantName
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(t => t.FontSize(9));

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
                    c.Item().Text("Stock Valuation Report").FontSize(11).FontColor(Colors.Grey.Darken2);
                });
                row.ConstantItem(180).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text($"Grand total: {_data.Currency} {Money(_data.GrandTotal)}").Bold();
                    c.Item().AlignRight().Text($"{_data.Lines.Count} item(s)").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });
            col.Item().PaddingVertical(6).LineHorizontal(0.7f).LineColor(Colors.Grey.Lighten1);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(6).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2);   // category
                c.RelativeColumn(3);   // item
                c.ConstantColumn(70);  // on hand
                c.ConstantColumn(60);  // avg cost
                c.ConstantColumn(70);  // value
            });

            table.Header(h =>
            {
                h.Cell().Text("Category").Bold();
                h.Cell().Text("Item").Bold();
                h.Cell().AlignRight().Text("On hand").Bold();
                h.Cell().AlignRight().Text("Avg cost").Bold();
                h.Cell().AlignRight().Text("Value").Bold();
            });

            foreach (var line in _data.Lines)
            {
                table.Cell().Text(line.Category).FontColor(Colors.Grey.Darken1);
                table.Cell().Text(t =>
                {
                    t.Span(line.Name);
                    if (line.IsLowStock) t.Span("  (low)").FontColor(Colors.Red.Darken1).FontSize(8);
                });
                table.Cell().AlignRight().Text($"{line.QtyOnHand:0.###} {line.UnitCode}");
                table.Cell().AlignRight().Text(Money(line.AvgCost));
                table.Cell().AlignRight().Text(Money(line.StockValue));
            }
        });
    }

    private static string Money(decimal v) => v.ToString("0.00");
}
