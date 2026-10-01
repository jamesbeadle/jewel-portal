using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>The week as every labour read names it: Monday to Sunday, keyed by its Monday at
/// midnight UTC, the way every labour date is stored.</summary>
public static class LabourWeeks
{
    public const int DaysInWeek = 7;
    public const int WorkingDaysInWeek = 5;

    public static DateTimeOffset MondayOf(DateTimeOffset moment)
    {
        var workDate = SiteClock.WorkDateOf(moment);
        return new DateTimeOffset(ForecastRules.WeekStartOf(workDate.UtcDateTime), TimeSpan.Zero);
    }

    public static DateTimeOffset SundayOf(DateTimeOffset moment) => MondayOf(moment).AddDays(DaysInWeek - 1);

    public static DateTimeOffset FridayOf(DateTimeOffset moment) => MondayOf(moment).AddDays(WorkingDaysInWeek - 1);

    public static bool IsWorkingDay(DateTimeOffset day) => day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    public static IEnumerable<DateTimeOffset> Between(DateTimeOffset from, DateTimeOffset to)
    {
        for (var day = from; day <= to; day = day.AddDays(1)) yield return day;
    }

    public static IEnumerable<DateTimeOffset> WorkingDaysOf(DateTimeOffset monday) =>
        Between(monday, monday.AddDays(WorkingDaysInWeek - 1));

    public static DateTimeOffset Earlier(DateTimeOffset first, DateTimeOffset second) => first < second ? first : second;
}
