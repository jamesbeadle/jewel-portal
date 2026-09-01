namespace Jewel.JPMS;

/// <summary>
/// The house date rendering, imported everywhere through the global usings (the MoneyFormats
/// pattern): "d MMM yyyy", with an em-dash for no date. Pages that render a DateTimeOffset
/// decide their own zone handling and keep a local overload — a naive date has nothing to
/// convert, so it belongs here.
/// </summary>
public static class DateFormats
{
    public static string DateText(DateTime? date) => date?.ToString("d MMM yyyy") ?? "—";
}
