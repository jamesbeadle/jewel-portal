namespace Jewel.JPMS;

/// <summary>
/// The house date rendering, imported everywhere through the global usings (the MoneyFormats
/// pattern): "d MMM yyyy", with an em-dash for no date — so no view types a format string.
/// DateTimeOffsets render in the browser's local time, which is what every page did by hand
/// before (`.LocalDateTime.ToString("d MMM yyyy")`); DateTimes and DateOnlys are naive and
/// render as they are. DateTimeText adds the time ("5 Sep 2026, 14:30").
/// </summary>
public static class DateFormats
{
    public const string DateFormat = "d MMM yyyy";
    public const string DateTimeFormat = "d MMM yyyy, HH:mm";
    public const string TimeFormat = "HH:mm";

    public static string DateText(DateTime? date) => date?.ToString(DateFormat) ?? "—";
    public static string DateText(DateTime date) => date.ToString(DateFormat);
    public static string DateText(DateTimeOffset? date) => date?.LocalDateTime.ToString(DateFormat) ?? "—";
    public static string DateText(DateTimeOffset date) => date.LocalDateTime.ToString(DateFormat);
    public static string DateText(DateOnly? date) => date?.ToString(DateFormat) ?? "—";
    public static string DateText(DateOnly date) => date.ToString(DateFormat);

    public static string DateTimeText(DateTime? date) => date?.ToString(DateTimeFormat) ?? "—";
    public static string DateTimeText(DateTime date) => date.ToString(DateTimeFormat);
    public static string DateTimeText(DateTimeOffset? date) => date?.LocalDateTime.ToString(DateTimeFormat) ?? "—";
    public static string DateTimeText(DateTimeOffset date) => date.LocalDateTime.ToString(DateTimeFormat);

    public static string TimeText(DateTime date) => date.ToString(TimeFormat);
    public static string TimeText(DateTimeOffset date) => date.LocalDateTime.ToString(TimeFormat);
}
