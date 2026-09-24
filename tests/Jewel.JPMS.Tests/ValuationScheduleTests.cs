using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

public class ValuationScheduleTests
{
    private static readonly DateTimeOffset ByFranceAnchor = Day(2026, 9, 23);

    private static DateTimeOffset Day(int year, int month, int day) => new(year, month, day, 0, 0, 0, TimeSpan.Zero);

    private static DateTimeOffset? Monthly(DateTimeOffset? lockedAt) =>
        ValuationSchedule.NextDue(ByFranceAnchor, ValuationCycle.Monthly, lockedAt);

    [Fact]
    public void ByFrance_lockedOnTheDay_isNextDueTheSameDateNextMonth()
    {
        var lockedAt = new DateTimeOffset(2026, 9, 23, 8, 28, 0, TimeSpan.FromHours(1));
        Assert.Equal(Day(2026, 10, 23), Monthly(lockedAt));
    }

    [Fact]
    public void AClaimLockedAFewDaysEarly_countsForItsPeriod()
    {
        Assert.Equal(Day(2026, 10, 23), Monthly(Day(2026, 9, 19)));
    }

    [Fact]
    public void AClaimLockedLate_countsForThePeriodItMissed_andTheNextIsStillExpected()
    {
        Assert.Equal(Day(2026, 10, 23), Monthly(Day(2026, 10, 10)));
    }

    [Fact]
    public void ALockFromAPreviousPeriod_leavesTheAnchorDue()
    {
        Assert.Equal(ByFranceAnchor, Monthly(Day(2026, 8, 24)));
    }

    [Fact]
    public void NoClaimLocked_isTheAnchor()
    {
        Assert.Equal(ByFranceAnchor, Monthly(null));
    }

    [Fact]
    public void NoCycle_isTheDateAsSetByHand_whateverIsLocked()
    {
        var next = ValuationSchedule.NextDue(ByFranceAnchor, ValuationCycle.None, Day(2026, 9, 23));
        Assert.Equal(ByFranceAnchor, next);
    }

    [Fact]
    public void NoDate_isNotSet()
    {
        Assert.Null(ValuationSchedule.NextDue(null, ValuationCycle.Monthly, Day(2026, 9, 23)));
    }

    [Fact]
    public void Fortnightly_stepsFourteenDays()
    {
        var next = ValuationSchedule.NextDue(ByFranceAnchor, ValuationCycle.Fortnightly, Day(2026, 9, 23));
        Assert.Equal(Day(2026, 10, 7), next);
    }

    [Fact]
    public void FourWeekly_afterSeveralLocks_isTheCycleAfterTheNewest()
    {
        var next = ValuationSchedule.NextDue(ByFranceAnchor, ValuationCycle.FourWeekly, Day(2026, 11, 18));
        Assert.Equal(Day(2026, 12, 16), next);
    }

    [Fact]
    public void Monthly_fromTheThirtyFirst_keepsTheDateWhereTheMonthHasIt()
    {
        var next = ValuationSchedule.NextDue(Day(2026, 1, 31), ValuationCycle.Monthly, Day(2026, 2, 27));
        Assert.Equal(Day(2026, 3, 31), next);
    }
}
