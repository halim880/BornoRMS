using BornoBit.Restaurant.Application.Common.Time;

namespace BornoBit.Restaurant.Infrastructure.Time;

/// <summary>
/// <see cref="IBusinessClock"/> backed by a configured IANA/Windows timezone id (default "Asia/Dhaka",
/// from the shared <c>Receipt:TimeZoneId</c> setting). Converts local business-day boundaries to UTC so
/// day filters match how orders are actually stamped (<c>OrderedAtUtc</c> in UTC).
/// </summary>
public sealed class BusinessClock : IBusinessClock
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;

    public BusinessClock(TimeProvider timeProvider, string? timeZoneId)
    {
        _timeProvider = timeProvider;
        _timeZone = ResolveTimeZone(timeZoneId);
    }

    public DateOnly Today
    {
        get
        {
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(_timeProvider.GetUtcNow().UtcDateTime, _timeZone);
            return DateOnly.FromDateTime(localNow);
        }
    }

    public (DateTime FromUtc, DateTime ToUtc) DayWindowUtc(DateOnly businessDate)
        => RangeUtc(businessDate, businessDate);

    public (DateTime FromUtc, DateTime ToUtc) RangeUtc(DateOnly fromInclusive, DateOnly toInclusive)
    {
        var localStart = DateTime.SpecifyKind(fromInclusive.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var localEnd = DateTime.SpecifyKind(toInclusive.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return (TimeZoneInfo.ConvertTimeToUtc(localStart, _timeZone),
                TimeZoneInfo.ConvertTimeToUtc(localEnd, _timeZone));
    }

    public DateOnly ToBusinessDate(DateTime utc)
    {
        var asUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(asUtc, _timeZone));
    }

    private static TimeZoneInfo ResolveTimeZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return TimeZoneInfo.Utc;
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }
}