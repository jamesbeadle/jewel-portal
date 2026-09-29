namespace Jewel.JPMS.Features.Labour.MyDay;

/// <summary>Hours as the worker reads them — "8 h", "4.5 h" — one format for the picker, the
/// logged card and the week, never the decimal's four places.</summary>
public static class MyDayHoursText
{
    private const string HoursFormat = "0.#";

    public static string Of(decimal hours) => $"{hours.ToString(HoursFormat)} h";

    public static string Bare(decimal hours) => hours.ToString(HoursFormat);
}
