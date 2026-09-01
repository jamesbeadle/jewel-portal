using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Triage;

/// <summary>How an email reads in triage lists and panes: senders, dates, previews and tag labels.</summary>
public static class TriageEmailDisplay
{
    /// <summary>Display label for a workflow tag chip: drop the "JPMS/" prefix (e.g. "JPMS/RFI-001" → "RFI-001").</summary>
    public static string TagLabel(string tag) =>
        tag.StartsWith("JPMS/", StringComparison.OrdinalIgnoreCase) ? tag["JPMS/".Length..] : tag;

    public static string DisplayFrom(MailboxMessage item) =>
        string.IsNullOrWhiteSpace(item.FromName) ? item.FromEmail : item.FromName;

    public static string Dash(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

    public static string Date(DateTimeOffset value) => value.LocalDateTime.ToString("d MMM yyyy, HH:mm");

    /// <summary>
    /// Outlook-style compact date for list rows: time alone today, "Yesterday 14:21", day name
    /// within the week, then the date.
    /// </summary>
    public static string ListDate(DateTimeOffset value)
    {
        var local = value.LocalDateTime;
        var today = DateTime.Now.Date;
        if (local.Date == today) return local.ToString("HH:mm");
        if (local.Date == today.AddDays(-1)) return $"Yesterday {local:HH:mm}";
        if (local.Date > today.AddDays(-6)) return local.ToString("ddd HH:mm");
        return local.ToString("d MMM yyyy");
    }

    /// <summary>The group header a list row falls under — Today / Yesterday / day names this week / month.</summary>
    public static string DateGroupLabel(DateTimeOffset value)
    {
        var local = value.LocalDateTime;
        var today = DateTime.Now.Date;
        if (local.Date == today) return "Today";
        if (local.Date == today.AddDays(-1)) return "Yesterday";
        if (local.Date > today.AddDays(-6)) return local.ToString("dddd");
        return local.ToString("MMMM yyyy");
    }

    /// <summary>
    /// Graph's bodyPreview can open with boilerplate line breaks; the row preview wants the first
    /// line with any content, whitespace collapsed.
    /// </summary>
    public static string FirstLineOf(string preview)
    {
        var line = preview.Replace("\r\n", "\n").Split('\n')
            .Select(candidate => candidate.Trim())
            .FirstOrDefault(candidate => candidate.Length > 0) ?? "";
        return System.Text.RegularExpressions.Regex.Replace(line, "\\s+", " ");
    }
}
