using BornoBit.Restaurant.Reporting.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BornoBit.Restaurant.Reporting.Documents;

public class StoreIssueVoucherDocument : IDocument
{
    private readonly StoreIssueVoucherReportData _data;

    public StoreIssueVoucherDocument(StoreIssueVoucherReportData data) { _data = data; }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Store Issue {_data.IssueNumber}",
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
                    c.Item().Text("Store Issue Voucher").FontSize(11).FontColor(Colors.Grey.Darken2);
                });
                row.ConstantItem(170).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text(_data.IssueNumber).Bold();
                    c.Item().AlignRight().Text($"{_data.IssuedAtUtc:yyyy-MM-dd HH:mm} UTC").FontSize(9).FontColor(Colors.Grey.Darken1);
                    c.Item().AlignRight().Text(_data.Status).FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });

            col.Item().PaddingTop(6).Text(t =>
            {
                t.Span("Destination: ").SemiBold();
                t.Span(_data.Destination);
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
                    c.RelativeColumn(5);   // item
                    c.ConstantColumn(110); // qty
                });

                table.Header(h =>
                {
                    h.Cell().Text("Item").Bold();
                    h.Cell().AlignRight().Text("Qty").Bold();
                });

                foreach (var line in _data.Lines)
                {
                    table.Cell().Text(line.ItemName);
                    table.Cell().AlignRight().Text($"{line.Qty:0.###} {line.UnitCode}");
                }
            });

            if (!string.IsNullOrWhiteSpace(_data.Notes))
            {
                col.Item().PaddingTop(10).Text(t =>
                {
                    t.Span("Notes: ").SemiBold();
                    t.Span(_data.Notes).FontColor(Colors.Grey.Darken2);
                });
            }

            col.Item().PaddingTop(28).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().LineHorizontal(0.6f).LineColor(Colors.Grey.Darken1);
                    c.Item().PaddingTop(3).Text("Issued by").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
                row.ConstantItem(30);
                row.RelativeItem().Column(c =>
                {
                    c.Item().LineHorizontal(0.6f).LineColor(Colors.Grey.Darken1);
                    c.Item().PaddingTop(3).Text("Received by").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });
        });
    }
}
