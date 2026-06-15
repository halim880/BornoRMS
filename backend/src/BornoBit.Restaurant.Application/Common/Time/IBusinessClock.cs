namespace BornoBit.Restaurant.Application.Common.Time;

/// <summary>
/// Resolves "business day" boundaries against the restaurant's configured local timezone.
/// Order timestamps are stored in UTC, but cashiers, dashboards and reports reason in local
/// calendar days — this converts a local <see cref="DateOnly"/> into the matching UTC window so
/// day filters don't silently drop orders placed in the early-morning local hours.
/// </summary>
public interface IBusinessClock
{
    /// <summary>The current date in the business timezone (use instead of <c>DateTime.Now/UtcNow</c> for defaults).</summary>
    DateOnly Today { get; }

    /// <summary>The half-open UTC range <c>[FromUtc, ToUtc)</c> covering the given local business day.</summary>
    (DateTime FromUtc, DateTime ToUtc) DayWindowUtc(DateOnly businessDate);

    /// <summary>The half-open UTC range covering local days <paramref name="fromInclusive"/>..<paramref name="toInclusive"/> (inclusive of both).</summary>
    (DateTime FromUtc, DateTime ToUtc) RangeUtc(DateOnly fromInclusive, DateOnly toInclusive);

    /// <summary>The local business <see cref="DateOnly"/> a UTC instant falls on (for day-bucketing report rows).</summary>
    DateOnly ToBusinessDate(DateTime utc);
}