using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Accounting;

/// <summary>
/// A calendar-month accounting period. Once <see cref="FiscalPeriodStatus.Closed"/>, postings dated
/// inside it are rejected by the period guard (back-dating into a closed month is the thing we prevent).
/// A period with no row is implicitly Open, so the guard is lenient by default.
/// </summary>
public class FiscalPeriod : AuditableEntity
{
    public int Year { get; private set; }
    public int Month { get; private set; }
    public string Name { get; private set; } = default!;
    public FiscalPeriodStatus Status { get; private set; } = FiscalPeriodStatus.Open;
    public DateTime? ClosedAtUtc { get; private set; }
    public string? ClosedBy { get; private set; }

    private FiscalPeriod() { }

    public static FiscalPeriod Create(int year, int month)
    {
        if (year < 2000 || year > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month));

        return new FiscalPeriod
        {
            Year = year,
            Month = month,
            Name = $"{year:0000}-{month:00}",
            Status = FiscalPeriodStatus.Open
        };
    }

    public bool Contains(DateTime date) => date.Year == Year && date.Month == Month;

    public void Close(DateTime nowUtc, string? by)
    {
        if (Status == FiscalPeriodStatus.Closed) throw new InvalidOperationException($"Period {Name} is already closed.");
        Status = FiscalPeriodStatus.Closed;
        ClosedAtUtc = nowUtc;
        ClosedBy = by;
    }

    public void Reopen()
    {
        if (Status == FiscalPeriodStatus.Open) throw new InvalidOperationException($"Period {Name} is already open.");
        Status = FiscalPeriodStatus.Open;
        ClosedAtUtc = null;
        ClosedBy = null;
    }
}
