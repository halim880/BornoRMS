using BornoBit.Restaurant.Reporting.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BornoBit.Restaurant.Reporting.Documents;

public class StoreMovementLedgerDocument : IDocument
{
    private readonly StoreMovementLedgerReportData _data;
    private readonly bool _singleItem;

    public StoreMovementLedgerDocument(StoreMovementLedgerReportData data)
    {
        _data = data;
        _singleItem = !string.IsNullOrWhiteSpace(_data.ItemName);
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = "Store Movement Ledger",
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
                    c.Item().Text("Store Movement Ledger").FontSize(11).FontColor(Colors.Grey.Darken2);
                    c.Item().Text(RangeLabel()).FontSize(8).FontColor(Colors.Grey.Darken1);
                });
                row.ConstantItem(180).AlignRight().Column(c =>
                {
                    if (_singleItem)
                    {
                        c.Item().AlignRight().Text(_data.ItemName).Bold();
                        if (_data.OpeningBalance is { } ob)
                            c.Item().AlignRight().Text($"Opening: {ob:0.###} {_data.UnitCode}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        if (_data.ClosingBalance is { } cb)
                            c.Item().AlignRight().Text($"Closing: {cb:0.###} {_data.UnitCode}").FontSize(9).Bold();
                    }
                    c.Item().AlignRight().Text($"{_data.Lines.Count} movement(s)").FontSize(9).FontColor(Colors.Grey.Darken1);
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
                c.ConstantColumn(95);  // date
                if (!_singleItem) c.RelativeColumn(3); // item
                c.ConstantColumn(85);  // type
                c.ConstantColumn(70);  // qty
                c.RelativeColumn(3);   // reason
                if (_singleItem) c.ConstantColumn(75); // running
            });

            table.Header(h =>
            {
                h.Cell().Text("Date (UTC)").Bold();
                if (!_singleItem) h.Cell().Text("Item").Bold();
                h.Cell().Text("Type").Bold();
                h.Cell().AlignRight().Text("Qty").Bold();
                h.Cell().Text("Reason").Bold();
                if (_singleItem) h.Cell().AlignRight().Text("Balance").Bold();
            });

            foreach (var line in _data.Lines)
            {
                table.Cell().Text($"{line.OccurredAtUtc:yyyy-MM-dd HH:mm}");
                if (!_singleItem) table.Cell().Text(line.ItemName);
                table.Cell().Text(line.MovementType).FontColor(Colors.Grey.Darken1);
                table.Cell().AlignRight().Text(t =>
                {
                    var sign = line.QtyBase >= 0 ? "+" : "";
                    t.Span($"{sign}{line.QtyBase:0.###} {line.UnitCode}")
                        .FontColor(line.QtyBase >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken1);
                });
                table.Cell().Text(line.Reason ?? "").FontColor(Colors.Grey.Darken1).FontSize(8);
                if (_singleItem)
                    table.Cell().AlignRight().Text(line.RunningBalance is { } rb ? $"{rb:0.###}" : "");
            }
        });
    }

    private string RangeLabel()
    {
        var from = _data.FromUtc is { } f ? f.ToString("yyyy-MM-dd") : "start";
        var to = _data.ToUtc is { } t ? t.ToString("yyyy-MM-dd") : "now";
        return $"Range: {from} → {to}";
    }
}
