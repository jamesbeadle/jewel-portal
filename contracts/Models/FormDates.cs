using System.Globalization;

namespace Jewel.JPMS.Models;

/// <summary>A date as a form's date box sends it (yyyy-MM-dd), read and written the one way everywhere.</summary>
public static class FormDates
{
    public const string AnswerFormat = "yyyy-MM-dd";

    public static DateOnly? Read(string? answer) =>
        DateOnly.TryParseExact(answer?.Trim(), AnswerFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    public static string Write(DateOnly? date) => date?.ToString(AnswerFormat, CultureInfo.InvariantCulture) ?? "";

    /// <summary>A date as the start of that day in UTC, the way a certificate's expiry is stored on the directory.</summary>
    public static DateTimeOffset? AsMidnight(DateOnly? date) =>
        date is { } day ? new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;
}
