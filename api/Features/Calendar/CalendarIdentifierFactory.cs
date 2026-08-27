namespace Jewel.JPMS.Api.Features.Calendar;

internal static class CalendarIdentifierFactory
{
    public static string Next() => Guid.NewGuid().ToString("N");
}
