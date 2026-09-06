namespace Jewel.JPMS.Features.DocumentControl;

/// <summary>How a triage item reads on the Document Triage page — its sender, dates and which
/// files can be previewed inline.</summary>
public static class DocumentControlDisplay
{
    public static string DisplaySender(DocumentControlItem item) =>
        !string.IsNullOrWhiteSpace(item.FromName) ? item.FromName
        : !string.IsNullOrWhiteSpace(item.FromEmail) ? item.FromEmail
        : "Unknown sender";

    public static bool IsPdf(DocumentControlItem item) =>
        (item.ContentType ?? "").Contains("pdf", StringComparison.OrdinalIgnoreCase)
        || item.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    public static bool IsImage(DocumentControlItem item) =>
        (item.ContentType ?? "").StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    public static bool IsZip(DocumentControlItem item) =>
        item.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
        || (item.ContentType ?? "").Contains("zip", StringComparison.OrdinalIgnoreCase);

    public static string Date(DateTimeOffset value) => DateTimeText(value);

    public static string ListDate(DateTimeOffset value)
    {
        var local = value.LocalDateTime;
        var today = DateTime.Today;
        if (local.Date == today) return local.ToString("HH:mm");
        if (local.Date == today.AddDays(-1)) return $"Yesterday {local:HH:mm}";
        if (local.Date > today.AddDays(-6)) return local.ToString("ddd HH:mm");
        return DateText(local);
    }
}
