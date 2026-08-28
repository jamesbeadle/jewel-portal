namespace Jewel.JPMS.Contracts.Calendar;

/// <summary>
/// The one reading of start-time text, shared by the client forms and the API rules, so what a
/// person types ("8:00", "0800", "8.30am", "08:00:00") lands as the same canonical "HH:mm"
/// everywhere — or is refused the same way everywhere, client-side first. Blank means all-day
/// and normalises to null.
/// </summary>
public static class CalendarStartTime
{
    /// <summary>True with the canonical "HH:mm" (null for blank = all-day) when the text reads
    /// as a wall-clock time; false when it doesn't — a range like "8-9am" is refused rather
    /// than guessed at.</summary>
    public static bool TryNormalise(string? text, out string? normalised)
    {
        normalised = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        var value = text.Trim().ToLowerInvariant();

        var pm = value.EndsWith("pm", StringComparison.Ordinal);
        var am = value.EndsWith("am", StringComparison.Ordinal);
        if (pm || am) value = value[..^2].Trim();
        if (value.Length == 0) return false;

        string hourPart, minutePart;
        var separator = value.IndexOfAny(TimeSeparators);
        if (separator >= 0)
        {
            hourPart = value[..separator];
            minutePart = value[(separator + 1)..];
            // A seconds part ("08:00:00") is allowed and dropped.
            var seconds = minutePart.IndexOfAny(TimeSeparators);
            if (seconds >= 0) minutePart = minutePart[..seconds];
        }
        else if (value.Length == 4 && value.All(char.IsAsciiDigit))
        {
            // "0800" — the four-digit shorthand.
            hourPart = value[..2];
            minutePart = value[2..];
        }
        else
        {
            // A bare hour — "8", "8am".
            hourPart = value;
            minutePart = "0";
        }

        if (hourPart.Length is 0 or > 2 || minutePart.Length is 0 or > 2) return false;
        if (!int.TryParse(hourPart, out var hour) || !int.TryParse(minutePart, out var minute)) return false;
        if (pm && hour is >= 1 and <= 11) hour += 12;
        if (am && hour == 12) hour = 0;
        if (hour is < 0 or > 23 || minute is < 0 or > 59) return false;

        normalised = $"{hour:00}:{minute:00}";
        return true;
    }

    private static readonly char[] TimeSeparators = { ':', '.' };
}
