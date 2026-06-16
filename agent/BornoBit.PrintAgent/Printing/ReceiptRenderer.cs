using System.Globalization;

namespace BornoBit.PrintAgent.Printing;

/// <summary>Renders a <see cref="ReceiptPayload"/> to ESC/POS bytes for an 80mm/58mm thermal printer.</summary>
public static class ReceiptRenderer
{
    public static byte[] Render(ReceiptPayload r, int width, bool openDrawer, bool fullCut, bool isReprint)
    {
        var e = new EscPosBuilder(width).Init();

        // ---- Header ----
        e.AlignCenter().Bold(true).Size(2, 2).Line(r.RestaurantName).Size(1, 1).Bold(false);
        if (!string.IsNullOrWhiteSpace(r.Address)) e.Line(r.Address);
        if (!string.IsNullOrWhiteSpace(r.Phone)) e.Line($"Tel: {r.Phone}");
        if (!string.IsNullOrWhiteSpace(r.VatRegistrationNo)) e.Line($"VAT: {r.VatRegistrationNo}");
        if (!string.IsNullOrWhiteSpace(r.Website)) e.Line(r.Website);

        e.AlignLeft().Rule();

        if (isReprint) e.AlignCenter().Bold(true).Line("** REPRINT **").Bold(false).AlignLeft();

        // ---- Order meta ----
        e.Line($"Order : {r.OrderNumber}");
        if (!string.IsNullOrWhiteSpace(r.OrderType)) e.Line($"Type  : {r.OrderType}");
        if (!string.IsNullOrWhiteSpace(r.TableNumber)) e.Line($"Table : {r.TableNumber}");
        if (!string.IsNullOrWhiteSpace(r.CustomerName)) e.Line($"Guest : {r.CustomerName}");
        if (!string.IsNullOrWhiteSpace(r.CashierName)) e.Line($"Cashier: {r.CashierName}");
        e.Line($"Date  : {FormatLocal(r.OrderedAtUtc, r.TimeZoneId)}");
        e.Rule();

        // ---- Lines ----
        // Row 1: "qty x name". Row 2: right-aligned line total. Keeps long names readable on narrow paper.
        foreach (var l in r.Lines)
        {
            e.LeftRight($"{l.Quantity} x {l.Name}", Money(l.LineTotal));
            if (l.Quantity > 1)
                e.Line($"    @ {Money(l.UnitPrice)}");
        }
        e.Rule();

        // ---- Totals ----
        e.LeftRight("Subtotal", Money(r.Subtotal));
        if (r.DiscountAmount != 0) e.LeftRight("Discount", "-" + Money(r.DiscountAmount));
        if (r.ServiceChargeAmount != 0) e.LeftRight("Service charge", Money(r.ServiceChargeAmount));
        if (r.VatAmount != 0) e.LeftRight("VAT", Money(r.VatAmount));
        if (r.RoundingAdjustment != 0) e.LeftRight("Rounding", Money(r.RoundingAdjustment));
        e.Bold(true).Size(1, 2).LeftRight("TOTAL", $"{r.Currency} {Money(r.Total)}".Trim()).Size(1, 1).Bold(false);

        // ---- Payment ----
        if (!string.IsNullOrWhiteSpace(r.PaymentMethod))
        {
            e.Rule();
            e.LeftRight("Paid via", r.PaymentMethod!);
            if (r.AmountTendered is { } tendered) e.LeftRight("Tendered", Money(tendered));
            if (r.ChangeGiven is { } change) e.LeftRight("Change", Money(change));
            e.Line(r.IsPaid ? "Status: PAID" : "Status: UNPAID");
        }

        if (!string.IsNullOrWhiteSpace(r.Notes))
        {
            e.Rule();
            e.Line($"Note: {r.Notes}");
        }

        // ---- Footer ----
        e.Rule();
        e.AlignCenter();
        if (!string.IsNullOrWhiteSpace(r.ThankYouLine)) e.Line(r.ThankYouLine);
        if (!string.IsNullOrWhiteSpace(r.VisitAgainLine)) e.Line(r.VisitAgainLine);
        e.AlignLeft();

        if (openDrawer) e.OpenDrawer();
        e.Cut(fullCut);
        return e.ToArray();
    }

    private static string Money(decimal d) => d.ToString("0.00", CultureInfo.InvariantCulture);

    internal static string FormatLocal(DateTime utc, string? timeZoneId)
    {
        var dt = utc;
        if (utc.Kind == DateTimeKind.Unspecified) dt = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        try
        {
            if (!string.IsNullOrWhiteSpace(timeZoneId))
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                dt = TimeZoneInfo.ConvertTimeFromUtc(dt.ToUniversalTime(), tz);
            }
        }
        catch (TimeZoneNotFoundException) { /* fall back to UTC */ }
        catch (InvalidTimeZoneException) { /* fall back to UTC */ }
        return dt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
    }
}
