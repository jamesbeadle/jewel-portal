
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

    public static string Date(DateTimeOffset value) => DateTimeText(value);

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
        return DateText(local);
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

    /// <summary>
    /// Avatar initials from the sender's display name ("Lorraine Proud" → "LP"), falling back to
    /// the first letter of the address.
    /// </summary>
    public static string SenderInitials(MailboxMessage item)
    {
        var name = string.IsNullOrWhiteSpace(item.FromName) ? item.FromEmail : item.FromName;
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var words = name.Split(new[] { ' ', '|', '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(word => char.IsLetter(word[0]))
            .Take(2)
            .Select(word => char.ToUpperInvariant(word[0]))
            .ToArray();
        return words.Length == 0 ? char.ToUpperInvariant(name.Trim()[0]).ToString() : new string(words);
    }


    /// <summary>Same previewable set as the drawing viewer: PDFs (the in-app viewer) and images.</summary>
    public static bool IsPreviewable(IntakeAttachment attachment) => IsPdf(attachment) || IsImage(attachment);

    /// <summary>
    /// A PDF by Graph's content type OR by file name. Senders' systems (a merchant's K8 export, a
    /// scanner, a forwarded file) often label a .pdf as application/octet-stream — the type alone
    /// hid the Preview button on real PDFs (2026-09-03), so the extension counts too.
    /// </summary>
    public static bool IsPdf(IntakeAttachment attachment) =>
        (attachment.ContentType ?? "").Contains("pdf", StringComparison.OrdinalIgnoreCase)
        || HasExtension(attachment.Name, ".pdf");

    /// <summary>An image by content type or by a browser-renderable image extension.</summary>
    public static bool IsImage(IntakeAttachment attachment) =>
        (attachment.ContentType ?? "").StartsWith("image/", StringComparison.OrdinalIgnoreCase)
        || HasExtension(attachment.Name, ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp", ".svg");

    private static bool HasExtension(string? name, params string[] extensions)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var trimmed = name.Trim();
        foreach (var extension in extensions)
            if (trimmed.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>Ids go in the query string, never the path — Graph ids don't survive a URL path segment.</summary>
    public static string AttachmentUrl(string messageId, IntakeAttachment attachment, bool inline) =>
        $"/api/mailbox/message/attachment?id={Uri.EscapeDataString(messageId)}"
        + $"&aid={Uri.EscapeDataString(attachment.Id)}{(inline ? "&inline=1" : "")}";
}
