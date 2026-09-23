namespace Jewel.JPMS.Api.Features.Forms;

/// <summary>
/// The forms' calendar is London's: a right-to-work check dated today, a ticket that expires today
/// and a retention date that falls today are all UK dates, whatever time zone the host runs in.
/// </summary>
public static class FormClock
{
    private static readonly TimeZoneInfo London = LondonTimeZone();

    public static DateTimeOffset InLondon(DateTimeOffset moment) => TimeZoneInfo.ConvertTime(moment, London);

    public static DateOnly Today() => DateOnly.FromDateTime(InLondon(DateTimeOffset.UtcNow).DateTime);

    private static TimeZoneInfo LondonTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/London"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"); }
    }
}
