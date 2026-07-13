namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>
/// Working-day arithmetic for labour tracking. Sites run on UK local time; the working date is
/// the UK-local calendar date, stored as midnight UTC so day-uniqueness comparisons are exact.
/// </summary>
public static class SiteClock
{
    private static readonly TimeZoneInfo UkTime = ResolveUkTimeZone();

    private static TimeZoneInfo ResolveUkTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/London"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"); }
    }

    public static DateTimeOffset WorkDateOf(DateTimeOffset moment) =>
        new(TimeZoneInfo.ConvertTime(moment, UkTime).Date, TimeSpan.Zero);

    public static DateTimeOffset Today() => WorkDateOf(DateTimeOffset.UtcNow);
}
