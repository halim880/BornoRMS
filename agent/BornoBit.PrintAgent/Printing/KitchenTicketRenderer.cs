namespace BornoBit.PrintAgent.Printing;

/// <summary>Renders a <see cref="KitchenTicketPayload"/> (KOT) to ESC/POS bytes — big, sparse, no prices.</summary>
public static class KitchenTicketRenderer
{
    public static byte[] Render(KitchenTicketPayload k, int width, bool fullCut)
    {
        var e = new EscPosBuilder(width).Init();

        e.AlignCenter().Bold(true).Size(2, 2)
         .Line(k.IsPriority ? "*** RUSH ***" : "KITCHEN");
        e.Size(1, 1).Line(k.TicketLabel ?? $"KOT - {k.OrderNumber}").Bold(false).AlignLeft();
        e.Rule();

        if (!string.IsNullOrWhiteSpace(k.OrderType)) e.Line($"Type : {k.OrderType}");
        if (!string.IsNullOrWhiteSpace(k.TableNumber)) e.Line($"Table: {k.TableNumber}");
        if (!string.IsNullOrWhiteSpace(k.CustomerName)) e.Line($"Guest: {k.CustomerName}");
        e.Line($"Time : {ReceiptRenderer.FormatLocal(k.OrderedAtUtc, k.TimeZoneId)}");
        e.Rule();

        foreach (var l in k.Lines)
        {
            // Quantity and item are the only thing kitchen staff scan for — print them double-height.
            e.Bold(true).Size(1, 2).Line($"{l.Quantity} x {l.Name}").Size(1, 1).Bold(false);
            if (!string.IsNullOrWhiteSpace(l.StationName)) e.Line($"   [{l.StationName}]");
            foreach (var m in l.Modifiers)
                e.Line($"   + {m}");
            if (!string.IsNullOrWhiteSpace(l.Notes))
                e.Line($"   * {l.Notes}");
        }

        e.Rule();
        if (!string.IsNullOrWhiteSpace(k.KitchenNotes)) e.Bold(true).Line($"Kitchen: {k.KitchenNotes}").Bold(false);
        if (!string.IsNullOrWhiteSpace(k.Notes)) e.Line($"Order note: {k.Notes}");

        e.Cut(fullCut);
        return e.ToArray();
    }
}
