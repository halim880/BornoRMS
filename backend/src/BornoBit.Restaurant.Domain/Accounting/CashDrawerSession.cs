using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Accounting;

/// <summary>
/// One cashier's cash-drawer shift: opened with a float, accumulates cash received and cash paid out
/// (refunds / petty cash), then closed against a physical count to surface the variance. Reconciles
/// into a Cash-kind <see cref="CashAccount"/>. One open shift per cashier (enforced at the handler).
/// </summary>
public class CashDrawerSession : AuditableEntity
{
    public string DrawerNumber { get; private set; } = default!;
    public Guid CashierUserId { get; private set; }
    public string CashierName { get; private set; } = default!;
    public Guid CashAccountId { get; private set; }

    public decimal OpeningBalance { get; private set; }
    public decimal CashReceived { get; private set; }
    public decimal CashPaidOut { get; private set; }
    public decimal? CountedClosingBalance { get; private set; }

    public DrawerStatus Status { get; private set; } = DrawerStatus.Open;
    public DateTime OpenedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }

    public string? OpenNotes { get; private set; }
    public string? CloseNotes { get; private set; }

    /// <summary>What the drawer should hold: float + cash in − cash out.</summary>
    public decimal ExpectedClosingBalance => OpeningBalance + CashReceived - CashPaidOut;
    /// <summary>Counted − expected. Positive = over, negative = short. Only meaningful once closed.</summary>
    public decimal Variance => (CountedClosingBalance ?? 0m) - ExpectedClosingBalance;

    private CashDrawerSession() { }

    public static CashDrawerSession Open(
        string drawerNumber,
        Guid cashierUserId,
        string cashierName,
        Guid cashAccountId,
        decimal openingBalance,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(drawerNumber)) throw new ArgumentException("Drawer number is required.", nameof(drawerNumber));
        if (cashierUserId == Guid.Empty) throw new ArgumentException("Cashier is required.", nameof(cashierUserId));
        if (string.IsNullOrWhiteSpace(cashierName)) throw new ArgumentException("Cashier name is required.", nameof(cashierName));
        if (cashAccountId == Guid.Empty) throw new ArgumentException("Cash account is required.", nameof(cashAccountId));
        if (openingBalance < 0m) throw new ArgumentOutOfRangeException(nameof(openingBalance));

        return new CashDrawerSession
        {
            DrawerNumber = drawerNumber.Trim().ToUpperInvariant(),
            CashierUserId = cashierUserId,
            CashierName = cashierName.Trim(),
            CashAccountId = cashAccountId,
            OpeningBalance = openingBalance,
            Status = DrawerStatus.Open,
            OpenedAtUtc = DateTime.UtcNow,
            OpenNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
    }

    public void RecordCashIn(decimal amount)
    {
        EnsureOpen();
        if (amount < 0m) throw new ArgumentOutOfRangeException(nameof(amount));
        CashReceived += amount;
    }

    public void RecordCashOut(decimal amount)
    {
        EnsureOpen();
        if (amount < 0m) throw new ArgumentOutOfRangeException(nameof(amount));
        CashPaidOut += amount;
    }

    public void Close(decimal countedBalance, string? notes = null)
    {
        EnsureOpen();
        if (countedBalance < 0m) throw new ArgumentOutOfRangeException(nameof(countedBalance));
        CountedClosingBalance = countedBalance;
        Status = DrawerStatus.Closed;
        ClosedAtUtc = DateTime.UtcNow;
        CloseNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    private void EnsureOpen()
    {
        if (Status != DrawerStatus.Open) throw new InvalidOperationException("The drawer shift is already closed.");
    }
}
