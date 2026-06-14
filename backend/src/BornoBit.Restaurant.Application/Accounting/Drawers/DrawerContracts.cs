using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Domain.Ordering;

namespace BornoBit.Restaurant.Application.Accounting.Drawers;

public record DrawerDto(
    Guid Id,
    string DrawerNumber,
    Guid CashierUserId,
    string CashierName,
    Guid CashAccountId,
    string? CashAccountName,
    decimal OpeningBalance,
    decimal CashReceived,
    decimal CashPaidOut,
    decimal ExpectedClosingBalance,
    decimal? CountedClosingBalance,
    decimal Variance,
    DrawerStatus Status,
    DateTime OpenedAtUtc,
    DateTime? ClosedAtUtc);

public record DrawerMethodLineDto(PaymentMethod Method, int Count, decimal Amount);

public record DrawerSummaryDto(DrawerDto Drawer, IReadOnlyList<DrawerMethodLineDto> ByMethod);

public record DrawerCloseResultDto(string DrawerNumber, decimal Expected, decimal Counted, decimal Variance);

public static class DrawerMapping
{
    public static DrawerDto ToDto(this CashDrawerSession d, string? cashAccountName = null) => new(
        d.Id, d.DrawerNumber, d.CashierUserId, d.CashierName, d.CashAccountId, cashAccountName,
        d.OpeningBalance, d.CashReceived, d.CashPaidOut, d.ExpectedClosingBalance,
        d.CountedClosingBalance, d.Variance, d.Status, d.OpenedAtUtc, d.ClosedAtUtc);
}
