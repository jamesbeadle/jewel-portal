using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Features.Labour.MyDay;

/// <summary>The dates My day's calendar pages by: the Monday of a week, the first of a month, and
/// how a week or a month is named in its heading.</summary>
public static class MyDayCalendarDates
{
    private const string DayOfWeekFormat = "ddd d";
    private const string WeekEndFormat = "ddd d MMM";
    private const string MonthFormat = "MMMM yyyy";
    private const int DaysInWeek = 7;

    public static DateTimeOffset TodayUtc()
    {
        var now = DateTime.UtcNow;
        return new DateTimeOffset(now.Date, TimeSpan.Zero);
    }

    public static DateTimeOffset MondayOf(DateTimeOffset date) =>
        new(ForecastRules.WeekStartOf(date.UtcDateTime), TimeSpan.Zero);

    public static DateTimeOffset NextMonday(DateTimeOffset monday) => monday.AddDays(DaysInWeek);

    public static DateTimeOffset PreviousMonday(DateTimeOffset monday) => monday.AddDays(-DaysInWeek);

    public static bool IsThisWeek(DateTimeOffset monday, DateTimeOffset today) => monday == MondayOf(today);

    public static string WeekHeading(DateTimeOffset monday) =>
        $"{monday.ToString(DayOfWeekFormat)} – {monday.AddDays(DaysInWeek - 1).ToString(WeekEndFormat)}";

    public static string WeekName(DateTimeOffset monday, DateTimeOffset today)
    {
        if (IsThisWeek(monday, today)) return "This week";
        if (NextMonday(monday) == MondayOf(today)) return "Last week";
        return "Earlier";
    }

    public static string MonthHeading(int year, int month) => new DateTime(year, month, 1).ToString(MonthFormat);

    public static (int Year, int Month) MonthAfter(int year, int month)
    {
        var next = new DateTime(year, month, 1).AddMonths(1);
        return (next.Year, next.Month);
    }

    public static (int Year, int Month) MonthBefore(int year, int month)
    {
        var previous = new DateTime(year, month, 1).AddMonths(-1);
        return (previous.Year, previous.Month);
    }

    public static bool IsThisMonth(int year, int month, DateTimeOffset today) => today.Year == year && today.Month == month;
}
