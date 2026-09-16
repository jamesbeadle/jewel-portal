using System.Globalization;
using System.Text.RegularExpressions;

namespace Jewel.JPMS.Api.Features.Progress.WhatsApp;

/// <summary>
/// One line of a WhatsApp export as either phone writes it — Android
/// <c>04/09/2026, 10:22 - James Everitt: text</c> or iPhone
/// <c>[04/09/2026, 10:22:31] James Everitt: text</c> — with the marks WhatsApp sprinkles in
/// (left-to-right marks, the narrow no-break space before am/pm) taken out first. Dates are
/// day/month/year, as a UK phone exports them.
/// </summary>
internal static class WhatsAppExportLine
{
    private static readonly Regex Stamp = new(
        @"^\[?(?<day>\d{1,2})/(?<month>\d{1,2})/(?<year>\d{2,4}),?\s+(?<hour>\d{1,2}):(?<minute>\d{2})(?::(?<second>\d{2}))?\s?(?<meridiem>[AaPp]\.?[Mm]\.?)?\]?\s+(?:-\s+)?(?<rest>.*)$",
        RegexOptions.Compiled);

    private static readonly Regex SenderAndText = new(
        @"^(?<sender>[^:]{1,80}?):\s(?<text>.*)$", RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly string[] InvisibleMarks = { "‎", "‏", "‪", "‬", "﻿" };

    public sealed record Parsed(DateTime SentAt, string? Sender, string Rest);

    /// <summary>The line's stamp and what follows it; null for a continuation line of the
    /// message before it. A stamped line with no "Sender:" is a system line (Sender null).</summary>
    public static Parsed? Parse(string rawLine)
    {
        var line = Clean(rawLine);
        var match = Stamp.Match(line);
        if (!match.Success) return null;
        var sentAt = ReadSentAt(match);
        if (sentAt is null) return null;

        var rest = match.Groups["rest"].Value;
        var body = SenderAndText.Match(rest);
        return body.Success
            ? new Parsed(sentAt.Value, body.Groups["sender"].Value.Trim(), body.Groups["text"].Value)
            : new Parsed(sentAt.Value, null, rest);
    }

    public static string Clean(string rawLine)
    {
        var line = rawLine.Replace(' ', ' ').Replace(' ', ' ');
        foreach (var mark in InvisibleMarks) line = line.Replace(mark, "");
        return line.TrimEnd();
    }

    private static DateTime? ReadSentAt(Match match)
    {
        var year = Number(match, "year");
        if (year < 100) year += 2000;
        var hour = Number(match, "hour");
        var meridiem = match.Groups["meridiem"].Value.Replace(".", "").ToUpperInvariant();
        if (meridiem == "PM" && hour < 12) hour += 12;
        if (meridiem == "AM" && hour == 12) hour = 0;
        var second = match.Groups["second"].Success ? Number(match, "second") : 0;
        try
        {
            return new DateTime(year, Number(match, "month"), Number(match, "day"), hour, Number(match, "minute"), second);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static int Number(Match match, string group) =>
        int.Parse(match.Groups[group].Value, CultureInfo.InvariantCulture);
}
