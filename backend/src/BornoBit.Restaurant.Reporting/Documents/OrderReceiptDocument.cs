using BornoBit.Restaurant.Reporting.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BornoBit.Restaurant.Reporting.Documents;

public class OrderReceiptDocument : IDocument
{
    private readonly OrderReceiptReportData _data;

    public OrderReceiptDocument(OrderReceiptReportData data) { _data = data; }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Invoice {_data.OrderNumber}",
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
            page.Footer().Element(ComposeFooter);
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
                    c.Item().Text("Invoice").FontSize(11).FontColor(Colors.Grey.Darken2);
                });
                row.ConstantItem(160).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text(_data.OrderNumber).Bold();
                    c.Item().AlignRight().Text($"{_data.OrderedAtUtc:yyyy-MM-dd HH:mm} UTC").FontSize(9).FontColor(Colors.Grey.Darken1);
                    c.Item().AlignRight().Text(_data.Status).FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });
            col.Item().PaddingVertical(6).LineHorizontal(0.7f).LineColor(Colors.Grey.Lighten1);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(8).Column(col =>
        {
            col.Spacing(8);

            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Customer").FontSize(9).FontColor(Colors.Grey.Darken2);
                    c.Item().Text(string.IsNullOrWhiteSpace(_data.CustomerName) ? _data.CustomerPhone : _data.CustomerName!).Bold();
                    c.Item().Text(_data.CustomerPhone).FontSize(9);
                });
                row.RelativeItem().AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text("Type").FontSize(9).FontColor(Colors.Grey.Darken2);
                    c.Item().AlignRight().Text(_data.OrderType).Bold();
                    if (!string.IsNullOrWhiteSpace(_data.TableNumber))
                        c.Item().AlignRight().Text($"Table {_data.TableNumber}").FontSize(9);
                });
            });

            col.Item().PaddingTop(6).Text("Items").Bold().FontSize(12);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(3);
                    c.ConstantColumn(40);
                    c.ConstantColumn(60);
                    c.ConstantColumn(60);
                });

                table.Header(h =>
                {
                    h.Cell().Text("Item").Bold();
                    h.Cell().AlignRight().Text("Qty").Bold();
                    h.Cell().AlignRight().Text("Price").Bold();
                    h.Cell().AlignRight().Text("Total").Bold();
                });

                foreach (var line in _data.Lines)
                {
                    table.Cell().Text(line.Name);
                    table.Cell().AlignRight().Text(line.Quantity.ToString());
                    table.Cell().AlignRight().Text(Money(line.UnitPrice));
                    table.Cell().AlignRight().Text(Money(line.LineTotal));
                }
            });

            col.Item().PaddingTop(6).LineHorizontal(0.7f).LineColor(Colors.Grey.Lighten1);

            TotalRow("Subtotal", $"{_data.Currency} {Money(_data.Subtotal)}", bold: false);
            if (_data.DiscountAmount > 0m)
                TotalRow("Discount", $"- {_data.Currency} {Money(_data.DiscountAmount)}", bold: false);
            TotalRow("Grand Total", $"{_data.Currency} {Money(_data.Total)}", bold: true);

            if (_data.IsPaid)
            {
                col.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem();
                    row.ConstantItem(220).Column(c =>
                    {
                        c.Item().AlignRight().Text($"PAID · {_data.PaymentMethod}").Bold().FontColor(Colors.Green.Darken2);
                        if (_data.AmountTendered is { } t)
                            c.Item().AlignRight().Text($"Tendered {_data.Currency} {Money(t)}").FontSize(9).FontColor(Colors.Grey.Darken2);
                        if (_data.ChangeGiven is { } ch)
                            c.Item().AlignRight().Text($"Change {_data.Currency} {Money(ch)}").FontSize(9).FontColor(Colors.Grey.Darken2);
                    });
                });
            }

            void TotalRow(string label, string value, bool bold)
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem();
                    row.ConstantItem(180).Row(r =>
                    {
                        var lbl = r.RelativeItem().Text(label);
                        var val = r.RelativeItem().AlignRight().Text(value);
                        if (bold) { lbl.Bold().FontSize(12); val.Bold().FontSize(12); }
                    });
                });
            }

            if (!string.IsNullOrWhiteSpace(_data.Notes))
                col.Item().PaddingTop(6).Text($"Notes: {_data.Notes}").FontSize(9).FontColor(Colors.Grey.Darken2);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            col.Item().AlignCenter().Text("Thank you for dining with us!").FontSize(9).FontColor(Colors.Grey.Darken1);
            col.Item().AlignCenter().Text($"Generated {_data.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC").FontSize(8).FontColor(Colors.Grey.Medium);
        });
    }

    private static string Money(decimal v) => v.ToString("0.##");
}
