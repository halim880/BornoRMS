using BornoBit.Restaurant.Reporting.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BornoBit.Restaurant.Reporting.Documents;

public class SalesInvoiceReportDocument : IDocument
{
    private readonly SalesInvoiceReportData _data;

    public SalesInvoiceReportDocument(SalesInvoiceReportData data) => _data = data;

    public DocumentMetadata GetMetadata() => new()
    {
        Title = "Sales Report",
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
                    c.Item().Text("Sales Report (Invoice-wise)").FontSize(11).FontColor(Colors.Grey.Darken2);
                    c.Item().Text($"Range: {_data.FromUtc:dd/MM/yyyy} → {_data.ToUtc:dd/MM/yyyy}").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
                row.ConstantItem(180).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text($"{_data.TotalInvoices} invoice(s)").FontSize(9).FontColor(Colors.Grey.Darken1);
                    c.Item().AlignRight().Text($"{_data.Currency} {Money(_data.GrandTotal)}").FontSize(11).Bold();
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
                c.ConstantColumn(95);   // date/time
                c.RelativeColumn(2);    // invoice #
                c.RelativeColumn(2);    // customer
                c.ConstantColumn(60);   // type
                c.ConstantColumn(55);   // method
                c.ConstantColumn(65);   // subtotal
                c.ConstantColumn(60);   // discount
                c.ConstantColumn(70);   // total
            });

            table.Header(h =>
            {
                static IContainer Head(IContainer c) =>
                    c.Background(Colors.Grey.Lighten3).PaddingVertical(5).PaddingHorizontal(4);

                Head(h.Cell()).Text("Date").Bold();
                Head(h.Cell()).Text("Invoice #").Bold();
                Head(h.Cell()).Text("Customer").Bold();
                Head(h.Cell()).Text("Type").Bold();
                Head(h.Cell()).Text("Method").Bold();
                Head(h.Cell()).AlignRight().Text("Subtotal").Bold();
                Head(h.Cell()).AlignRight().Text("Discount").Bold();
                Head(h.Cell()).AlignRight().Text("Total").Bold();
            });

            var i = 0;
            foreach (var r in _data.Rows)
            {
                var zebra = i++ % 2 == 1;
                IContainer Cell() => zebra
                    ? table.Cell().Background(Colors.Grey.Lighten5).PaddingVertical(4).PaddingHorizontal(4)
                    : table.Cell().PaddingVertical(4).PaddingHorizontal(4);

                Cell().Text($"{r.PaidAtUtc:dd/MM/yyyy HH:mm}").FontColor(Colors.Grey.Darken2);
                Cell().Text(r.OrderNumber).SemiBold();
                Cell().Text(r.CustomerName).FontColor(Colors.Grey.Darken1);
                Cell().Text(r.OrderType).FontColor(Colors.Grey.Darken1);
                Cell().Text(r.PaymentMethod).FontColor(Colors.Grey.Darken1);
                Cell().AlignRight().Text(Money(r.Subtotal));
                Cell().AlignRight().Text(r.Discount > 0m ? Money(r.Discount) : "—").FontColor(Colors.Red.Darken1);
                Cell().AlignRight().Text(Money(r.Total)).Bold();
            }

            // Totals band.
            static IContainer Total(IContainer c) =>
                c.Background(Colors.Grey.Lighten3).BorderTop(1.2f).BorderColor(Colors.Grey.Medium)
                    .PaddingVertical(6).PaddingHorizontal(4);

            Total(table.Cell().ColumnSpan(5)).Text($"Total · {_data.TotalInvoices} invoice(s)").Bold();
            Total(table.Cell()).AlignRight().Text(Money(_data.TotalSubtotal)).Bold();
            Total(table.Cell()).AlignRight().Text(Money(_data.TotalDiscount)).Bold().FontColor(Colors.Red.Darken1);
            Total(table.Cell()).AlignRight().Text(Money(_data.GrandTotal)).Bold().FontColor(Colors.Green.Darken2);
        });
    }

    private static string Money(decimal v) => v.ToString("#,##0.00");
}
