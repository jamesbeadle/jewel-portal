namespace Jewel.JPMS.Features.Labour.MyDay;

/// <summary>A sign-in or sign-out time as the phone's clock field holds it (the time now unless the
/// worker changes it) and as the day sends it: that time on the day in the phone's own zone —
/// today for a day being logged, the day's own date for one being amended — or nothing when the
/// field is blank, so the server takes the moment it receives the day.</summary>
public static class MyDayTimes
{
    private const string ClockFormat = "HH:mm";

    public static string Now() => DateTime.Now.ToString(ClockFormat);

    public static DateTimeOffset? TodayAt(string clock) => At(DateTime.Today, clock);

    public static DateTimeOffset? At(DateTimeOffset day, string clock) => At(day.Date, clock);

    private static DateTimeOffset? At(DateTime day, string clock)
    {
        var isGiven = TimeOnly.TryParseExact(clock, ClockFormat, out var time);
        if (!isGiven) return null;
        var moment = DateTime.SpecifyKind(day.Date, DateTimeKind.Local).Add(time.ToTimeSpan());
        return new DateTimeOffset(moment);
    }
}
