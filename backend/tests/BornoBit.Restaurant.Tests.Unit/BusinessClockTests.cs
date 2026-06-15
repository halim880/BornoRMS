using BornoBit.Restaurant.Infrastructure.Time;
using Xunit;

namespace BornoBit.Restaurant.Tests.Unit;

/// <summary>
/// Regression tests for the cash-counter "loads no orders" bug: a local business day must map to the
/// correct UTC window, otherwise early-morning (UTC+6) orders carrying the previous UTC date are dropped.
/// </summary>
public class BusinessClockTests
{
    // TimeProvider is only consulted by Today; the window/date conversions below don't use it.
    private static BusinessClock Dhaka() => new(TimeProvider.System, "Asia/Dhaka");

    [Fact]
    public void DayWindowUtc_ForDhaka_ShiftsBoundariesBackSixHours()
    {
        var (fromUtc, toUtc) = Dhaka().DayWindowUtc(new DateOnly(2026, 6, 15));

        Assert.Equal(new DateTime(2026, 6, 14, 18, 0, 0, DateTimeKind.Utc), fromUtc);
        Assert.Equal(new DateTime(2026, 6, 15, 18, 0, 0, DateTimeKind.Utc), toUtc);
    }

    [Fact]
    public void DayWindowUtc_Includes_EarlyMorningLocalOrder_StampedPreviousUtcDate()
    {
        // The exact failure from the bug report: an order placed 00:28 local on Jun 15 (= 18:28 UTC Jun 14).
        var orderedAtUtc = new DateTime(2026, 6, 14, 18, 28, 0, DateTimeKind.Utc);
        var (fromUtc, toUtc) = Dhaka().DayWindowUtc(new DateOnly(2026, 6, 15));

        Assert.True(orderedAtUtc >= fromUtc && orderedAtUtc < toUtc);
    }

    [Fact]
    public void RangeUtc_IsInclusiveOfBothLocalDays()
    {
        var (fromUtc, toUtc) = Dhaka().RangeUtc(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30));

        Assert.Equal(new DateTime(2026, 5, 31, 18, 0, 0, DateTimeKind.Utc), fromUtc);
        Assert.Equal(new DateTime(2026, 6, 30, 18, 0, 0, DateTimeKind.Utc), toUtc);
    }

    [Fact]
    public void ToBusinessDate_MapsUtcInstantToLocalDay()
    {
        // 18:28 UTC Jun 14 is 00:28 local Jun 15 in Dhaka.
        var local = Dhaka().ToBusinessDate(new DateTime(2026, 6, 14, 18, 28, 0, DateTimeKind.Utc));
        Assert.Equal(new DateOnly(2026, 6, 15), local);
    }

    [Fact]
    public void UnknownTimeZone_FallsBackToUtc()
    {
        var clock = new BusinessClock(TimeProvider.System, "Not/AZone");
        var (fromUtc, toUtc) = clock.DayWindowUtc(new DateOnly(2026, 6, 15));

        Assert.Equal(new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc), fromUtc);
        Assert.Equal(new DateTime(2026, 6, 16, 0, 0, 0, DateTimeKind.Utc), toUtc);
    }
}