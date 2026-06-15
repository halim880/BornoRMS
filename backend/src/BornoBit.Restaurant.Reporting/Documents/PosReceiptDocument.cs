using BornoBit.Restaurant.Reporting.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace BornoBit.Restaurant.Reporting.Documents;

/// <summary>
/// Receipt for a 58mm thermal POS printer (ESC/POS class): continuous page height,
/// monospaced glyphs so character dividers and columns align, pure black only
/// (thermal heads are 1-bit — grey dithers into noise), ~30 characters per line.
/// </summary>
public class PosReceiptDocument : IDocument
{
    // 58mm paper, 3mm margins -> 52mm (~147pt) printable. Courier New advances
    // 0.6em, so 8pt yields 4.8pt/char and 30 columns (144pt) fill the line.
    private const float PaperWidthMm = 58f;
    private const float MarginMm = 3f;
    private const int ColumnsPerLine = 30;
    private const float BaseFontSize = 8f;
    private const float SmallFontSize = 7f;
    private const float TitleFontSize = 11f;
    private const float EmphasisFontSize = 10f;
    private const float InfoLabelWidth = 40f;
    private const float QtyColumnWidth = 16f;
    private const float AmountColumnWidth = 44f;

    private static readonly string[] MonoFonts = { "Courier New", "Liberation Mono", "Consolas" };

    private readonly OrderReceiptReportData _data;
    private readonly ReceiptBranding _branding;
    private readonly TimeZoneInfo _timeZone;

    public PosReceiptDocument(OrderReceiptReportData data)
    {
        _data = data;
        _branding = data.Branding ?? new ReceiptBranding { Name = data.RestaurantName };
        _timeZone = ResolveTimeZone(_branding.TimeZoneId);
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Receipt {_data.OrderNumber}",
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
                col.Item().Element(ComposeTotals);
                col.Item().Element(ComposePaymentInfo);
                col.Item().Element(ComposeFooter);
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text(_branding.Name.ToUpperInvariant())
                .FontSize(TitleFontSize).Bold();

            if (!string.IsNullOrWhiteSpace(_branding.Address))
                col.Item().AlignCenter().Text(_branding.Address).FontSize(SmallFontSize);

            if (!string.IsNullOrWhiteSpace(_branding.Phone))
                col.Item().AlignCenter().Text($"Mobile: {_branding.Phone}").FontSize(SmallFontSize);

            if (!string.IsNullOrWhiteSpace(_branding.VatRegistrationNo))
                col.Item().AlignCenter().Text($"VAT Reg: {_branding.VatRegistrationNo}").FontSize(SmallFontSize);
        });
    }

    private void ComposeOrderInfo(IContainer container)
    {
        container.Column(col =>
        {
            InfoRow(col, "Invoice", _data.OrderNumber);
            InfoRow(col, "Date", FormatLocal(_data.OrderedAtUtc));
            InfoRow(col, "Type", OrderTypeLabel());

            if (!string.IsNullOrWhiteSpace(_data.TableNumber))
                InfoRow(col, "Table", _data.TableNumber!);

            if (!string.IsNullOrWhiteSpace(_data.CashierName))
                InfoRow(col, "Cashier", _data.CashierName!);

            if (_data.CustomerPhone is { Length: > 0 } and not "WALK-IN")
                InfoRow(col, "Customer", string.IsNullOrWhiteSpace(_data.CustomerName)
                    ? _data.CustomerPhone
                    : $"{_data.CustomerName}, {_data.CustomerPhone}");
        });
    }

    private void ComposeItemTable(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(1);
            Divider(col, '-');

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(QtyColumnWidth);
                    c.RelativeColumn();
                    c.ConstantColumn(AmountColumnWidth);
                });

                table.Header(h =>
                {
                    h.Cell().Text("QTY").Bold();
                    h.Cell().Text("ITEM").Bold();
                    h.Cell().AlignRight().Text("AMOUNT").Bold();
                    h.Cell().ColumnSpan(3).Text(Rule('-'));
                });

                foreach (var line in _data.Lines)
                {
                    table.Cell().Text(line.Quantity.ToString(CultureInfo.InvariantCulture));
                    table.Cell().PaddingRight(2).Text(line.Name);
                    table.Cell().AlignRight().Text(Money(line.LineTotal));

                    foreach (var modifier in line.Modifiers ?? Array.Empty<OrderReceiptModifier>())
                    {
                        var label = modifier.PriceDelta > 0
                            ? $"+ {modifier.Name} (+{Money(modifier.PriceDelta)})"
                            : $"+ {modifier.Name}";
                        table.Cell().Text(string.Empty);
                        table.Cell().ColumnSpan(2).Text(label).FontSize(SmallFontSize);
                    }

                    if (line.Quantity > 1)
                    {
                        table.Cell().Text(string.Empty);
                        table.Cell().ColumnSpan(2).Text($"@ {Money(line.UnitPrice)}").FontSize(SmallFontSize);
                    }
                }
            });
        });
    }

    private void ComposeTotals(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(1);

            Divider(col, '-');
            AmountRow(col, "TOTAL ITEMS", _data.Lines.Sum(l => l.Quantity).ToString(CultureInfo.InvariantCulture));
            Divider(col, '-');

            AmountRow(col, "SUBTOTAL", Money(_data.Subtotal));
            AmountRow(col, "DISCOUNT", _data.DiscountAmount > 0m ? $"-{Money(_data.DiscountAmount)}" : Money(0m));
            AmountRow(col, "VAT", Money(_data.VatAmount));
            AmountRow(col, "SERVICE", Money(_data.ServiceChargeAmount));

            if (_data.RoundingAdjustment != 0m)
                AmountRow(col, "ROUNDING", $"{(_data.RoundingAdjustment < 0m ? "-" : "+")}{Money(Math.Abs(_data.RoundingAdjustment))}");

            Divider(col, '-');
            AmountRow(col, "NET TOTAL", $"{_data.Currency} {Money(_data.Total)}", emphasized: true);
            Divider(col, '=');
        });
    }

    private void ComposePaymentInfo(IContainer container)
    {
        container.Column(col =>
        {
            if (_data.IsPaid)
            {
                InfoRow(col, "Payment", _data.PaymentMethod ?? "Paid");

                if (_data.AmountTendered is { } tendered)
                    InfoRow(col, "Paid", $"{_data.Currency} {Money(tendered)}");

                if (_data.ChangeGiven is { } change)
                    InfoRow(col, "Change", $"{_data.Currency} {Money(change)}");
            }
            else
            {
                col.Item().AlignCenter().Text("** UNPAID **").Bold().FontSize(EmphasisFontSize);
            }

            if (!string.IsNullOrWhiteSpace(_data.Notes))
                col.Item().Text($"Note: {_data.Notes}").FontSize(SmallFontSize);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            Divider(col, '-');

            col.Item().AlignCenter().Text(_branding.ThankYouLine).Bold().FontSize(SmallFontSize);
            col.Item().AlignCenter().Text(_branding.VisitAgainLine).FontSize(SmallFontSize);

            if (!string.IsNullOrWhiteSpace(_branding.Website))
                col.Item().AlignCenter().Text(_branding.Website).FontSize(SmallFontSize);

            col.Item().PaddingTop(2).AlignCenter()
                .Text($"Printed: {FormatLocal(_data.GeneratedAtUtc)}").FontSize(SmallFontSize);
        });
    }

    // --- helpers -----------------------------------------------------------

    private static void InfoRow(ColumnDescriptor col, string label, string value) =>
        col.Item().Row(row =>
        {
            row.ConstantItem(InfoLabelWidth).Text(label);
            row.RelativeItem().Text($": {value}");
        });

    private static void AmountRow(ColumnDescriptor col, string label, string value, bool emphasized = false) =>
        col.Item().Row(row =>
        {
            var lbl = row.RelativeItem().Text(label);
            var val = row.ConstantItem(AmountColumnWidth + 14).AlignRight().Text(value);
            if (emphasized)
            {
                lbl.Bold().FontSize(EmphasisFontSize);
                val.Bold().FontSize(EmphasisFontSize);
            }
        });

    private static void Divider(ColumnDescriptor col, char ch) =>
        col.Item().Text(Rule(ch));

    private static string Rule(char ch) => new(ch, ColumnsPerLine);

    private static string Money(decimal value) =>
        value.ToString("#,##0.##", CultureInfo.InvariantCulture);

    private string OrderTypeLabel() => _data.OrderType switch
    {
        "DineIn" => "Dine In",
        "Takeaway" => "Takeaway",
        "Delivery" => "Delivery",
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
