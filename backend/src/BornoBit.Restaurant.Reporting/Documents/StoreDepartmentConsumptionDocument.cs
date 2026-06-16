using BornoBit.Restaurant.Reporting.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BornoBit.Restaurant.Reporting.Documents;

public class StoreDepartmentConsumptionDocument : IDocument
{
    private readonly StoreDepartmentConsumptionReportData _data;

    public StoreDepartmentConsumptionDocument(StoreDepartmentConsumptionReportData data) { _data = data; }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = "Store Department Consumption",
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
                    c.Item().Text("Department Consumption Report").FontSize(11).FontColor(Colors.Grey.Darken2);
                    c.Item().Text($"{_data.FromUtc:yyyy-MM-dd} → {_data.ToUtc:yyyy-MM-dd} (UTC)").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
                row.ConstantItem(180).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text($"Grand total: {_data.Currency} {Money(_data.GrandTotalValue)}").Bold();
                    c.Item().AlignRight().Text($"{_data.Rows.Count} department(s)").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });
            col.Item().PaddingVertical(6).LineHorizontal(0.7f).LineColor(Colors.Grey.Lighten1);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            if (_data.Rows.Count == 0)
            {
                col.Item().PaddingTop(20).AlignCenter().Text("No consumption in this period.").FontColor(Colors.Grey.Darken1);
                return;
            }

            foreach (var row in _data.Rows)
            {
                col.Item().PaddingTop(10).Row(r =>
                {
                    r.RelativeItem().Text(row.DepartmentName).FontSize(11).Bold();
                    r.ConstantItem(140).AlignRight().Text($"{_data.Currency} {Money(row.TotalValue)}").Bold();
                });

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(4);   // item
                        c.ConstantColumn(110); // qty
                        c.ConstantColumn(80);  // value
                    });

                    table.Header(h =>
                    {
                        h.Cell().Text("Item").SemiBold();
                        h.Cell().AlignRight().Text("Qty").SemiBold();
                        h.Cell().AlignRight().Text("Value").SemiBold();
                    });

                    foreach (var item in row.Items)
                    {
                        table.Cell().Text(item.ItemName);
                        table.Cell().AlignRight().Text($"{item.QtyBase:0.###} {item.BaseUnitCode}");
                        table.Cell().AlignRight().Text(Money(item.Value));
                    }
                });
            }
        });
    }

    private static string Money(decimal v) => v.ToString("0.00");
}
