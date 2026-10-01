using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

/// <summary>The month's figures as the worker invoices from them: approved hours and the days they
/// make at the standard day (the office's own arithmetic, ForecastRules.HoursToDays), the days still
/// waiting on the office, the days recorded off, and the working days already gone with nothing on
/// them. A day with nothing counts once, not once per site.</summary>
public static class MyDayMonthTotals
{
    public static MyMonthTotals Of(IReadOnlyList<MyWeekDay> days, DateTimeOffset today)
    {
        var elapsed = days.Where(row => row.Date <= today).ToList();
        var approvedHours = elapsed.Where(row => row.Status == TimesheetStatus.Approved).Sum(row => row.Hours);
        var waiting = elapsed.Count(row => row.IsWaiting);
        var off = elapsed.Count(row => row.Kind == MyWeekDayKind.Off);
        var missing = elapsed.GroupBy(row => row.Date).Count(IsNothingAllDay);
        return new MyMonthTotals(approvedHours, ForecastRules.HoursToDays(approvedHours), waiting, off, missing);
    }

    private static bool IsNothingAllDay(IGrouping<DateTimeOffset, MyWeekDay> day) =>
        day.All(row => row.IsMissed && !row.IsPlannedOff);
}
