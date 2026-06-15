using BornoBit.Restaurant.Reporting.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace BornoBit.Restaurant.Reporting.Documents;

/// <summary>
/// Kitchen Order Ticket (KOT) for a 58mm thermal printer. Items and quantities only —
/// no prices — with oversized item text so cooks can read it at a glance from the line.
/// Monospaced glyphs keep the QTY column aligned; pure black for 1-bit thermal heads.
/// </summary>
public class KitchenTicketDocument : IDocument
{
    private const float PaperWidthMm = 58f;
    private const float MarginMm = 3f;
    private const int ColumnsPerLine = 30;
    private const float BaseFontSize = 9f;
    private const float SmallFontSize = 8f;
    private const float TitleFontSize = 14f;
    private const float SubtitleFontSize = 11f;
    private const float ItemFontSize = 12f;
    private const float QtyColumnWidth = 28f;
    private const float InfoLabelWidth = 40f;

    private static readonly string[] MonoFonts = { "Courier New", "Liberation Mono", "Consolas" };

    private readonly KitchenTicketReportData _data;
    private readonly ReceiptBranding _branding;
    private readonly TimeZoneInfo _timeZone;

    public KitchenTicketDocument(KitchenTicketReportData data)
    {
        _data = data;
        _branding = data.Branding ?? new ReceiptBranding { Name = "Kitchen" };
        _timeZone = ResolveTimeZone(_branding.TimeZoneId);
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"KOT {_data.OrderNumber}",
        Author = _branding.Name
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.ContinuousSize(PaperWidthMm, Unit.Millimetre);
            page.Margin(MarginMm, Unit.Millimetre);
            page.DefaultTextStyle(t => t
                .FontFamily(MonoFonts)
                .FontSize(BaseFontSize)
                .FontColor(Colors.Black)
                .LineHeight(1.1f));

            page.Content().Column(col =>
            {
                col.Spacing(1);

                col.Item().Element(ComposeHeader);
                Divider(col, '=');
                col.Item().Element(ComposeOrderInfo);
                col.Item().Element(ComposeItemTable);
                col.Item().Element(ComposeFooter);
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text("KITCHEN ORDER").FontSize(TitleFontSize).Bold();
            col.Item().AlignCenter().Text(_data.TicketLabel ?? _data.OrderNumber)
                .FontSize(SubtitleFontSize).Bold();
        });
    }

    private void ComposeOrderInfo(IContainer container)
    {
        container.Column(col =>
        {
            InfoRow(col, "Order", _data.OrderNumber);
            InfoRow(col, "Time", FormatLocal(_data.OrderedAtUtc));
            InfoRow(col, "Type", OrderTypeLabel());

            if (!string.IsNullOrWhiteSpace(_data.TableNumber))
                InfoRow(col, "Table", _data.TableNumber!);

            if (!string.IsNullOrWhiteSpace(_data.CustomerName))
                InfoRow(col, "Customer", _data.CustomerName!);

            if (!string.IsNullOrWhiteSpace(_data.CashierName))
                InfoRow(col, "By", _data.CashierName!);
        });
    }

    private void ComposeItemTable(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(2);
            Divider(col, '-');

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(QtyColumnWidth);
                    c.RelativeColumn();
                });

                table.Header(h =>
                {
                    h.Cell().Text("QTY").Bold().FontSize(SmallFontSize);
                    h.Cell().Text("ITEM").Bold().FontSize(SmallFontSize);
                    h.Cell().ColumnSpan(2).Text(Rule('-'));
                });

                foreach (var line in _data.Lines)
                {
                    table.Cell().Text($"{line.Quantity}x").FontSize(ItemFontSize).Bold();
                    table.Cell().Text(line.Name).FontSize(ItemFontSize).Bold();

                    foreach (var modifier in line.Modifiers ?? Array.Empty<string>())
                    {
                        table.Cell().Text(string.Empty);
                        table.Cell().Text($"+ {modifier}").FontSize(SmallFontSize);
                    }

                    if (!string.IsNullOrWhiteSpace(line.Notes))
                    {
                        table.Cell().Text(string.Empty);
                        table.Cell().Text($"> {line.Notes}").FontSize(SmallFontSize).Italic();
                    }
                }
            });

            Divider(col, '-');
            col.Item().Text($"TOTAL ITEMS: {_data.Lines.Sum(l => l.Quantity)}").Bold();
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            if (!string.IsNullOrWhiteSpace(_data.Notes))
            {
                Divider(col, '-');
                col.Item().Text("NOTES").Bold().FontSize(SmallFontSize);
                col.Item().Text(_data.Notes!).FontSize(BaseFontSize);
            }

            Divider(col, '=');
            col.Item().AlignCenter().Text($"Printed: {FormatLocal(_data.GeneratedAtUtc)}")
                .FontSize(SmallFontSize);
        });
    }

    // --- helpers -----------------------------------------------------------

    private static void InfoRow(ColumnDescriptor col, string label, string value) =>
        col.Item().Row(row =>
        {
            row.ConstantItem(InfoLabelWidth).Text(label);
            row.RelativeItem().Text($": {value}");
        });

    private static void Divider(ColumnDescriptor col, char ch) =>
        col.Item().Text(Rule(ch));

    private static string Rule(char ch) => new(ch, ColumnsPerLine);

    private string OrderTypeLabel() => _data.OrderType switch
    {
        "DineIn" => "Dine In",
        "Takeaway" => "Takeaway",
        "Delivery" => "Delivery",
        "Collection" => "Collection",
        _ => _data.OrderType
    };

    private string FormatLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _timeZone)
            .ToString("dd-MMM-yyyy hh:mm tt", CultureInfo.InvariantCulture);

    private static TimeZoneInfo ResolveTimeZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return TimeZoneInfo.Utc;
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }
}
