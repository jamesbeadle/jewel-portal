namespace Jewel.JPMS.Models;

// A locked claim is taken as sent, however it went (2026-09-24). A lock covers the latest due
// date up to a week after it, so a claim locked a few days early still counts for its period.
public static class ValuationSchedule
{
    public const int EarlyLockWindowDays = 7;

    private const int DaysInFortnight = 14;
    private const int DaysInFourWeeks = 28;
    private const int MonthsInYear = 12;

    public static DateTimeOffset? NextDue(
        DateTimeOffset? anchor, ValuationCycle cycle, DateTimeOffset? lastLockedAt)
    {
        if (anchor is not { } anchorMoment) return null;
        var anchorDate = anchorMoment.UtcDateTime.Date;
        if (cycle == ValuationCycle.None || lastLockedAt is not { } lockedAt) return AtMidnight(anchorDate);

        var lockCovers = lockedAt.UtcDateTime.Date.AddDays(EarlyLockWindowDays);
        var coveredStep = LatestStepOnOrBefore(anchorDate, cycle, lockCovers);
        var following = Step(anchorDate, cycle, coveredStep + 1);
        return AtMidnight(following > anchorDate ? following : anchorDate);
    }

    private static int LatestStepOnOrBefore(DateTime anchorDate, ValuationCycle cycle, DateTime target)
    {
        if (cycle == ValuationCycle.Monthly) return LatestMonthOnOrBefore(anchorDate, target);
        var cycleDays = DaysIn(cycle);
        var daysFromAnchor = (target - anchorDate).Days;
        return (int)Math.Floor(daysFromAnchor / (double)cycleDays);
    }

    private static int LatestMonthOnOrBefore(DateTime anchorDate, DateTime target)
    {
        var months = (target.Year - anchorDate.Year) * MonthsInYear + target.Month - anchorDate.Month;
        var isPastTarget = anchorDate.AddMonths(months) > target;
        return isPastTarget ? months - 1 : months;
    }

    private static DateTime Step(DateTime anchorDate, ValuationCycle cycle, int steps) =>
        cycle == ValuationCycle.Monthly
            ? anchorDate.AddMonths(steps)
            : anchorDate.AddDays(steps * DaysIn(cycle));

    private static int DaysIn(ValuationCycle cycle) =>
        cycle == ValuationCycle.Fortnightly ? DaysInFortnight : DaysInFourWeeks;

    private static DateTimeOffset AtMidnight(DateTime date) => new(date, TimeSpan.Zero);
}
