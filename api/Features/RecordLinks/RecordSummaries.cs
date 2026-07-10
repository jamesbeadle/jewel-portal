namespace Jewel.JPMS.Api.Features.RecordLinks;

// Produces the short LinkableRecord.Summary shown under a record's title in the triage picker.
// Clipped server-side so list payloads stay small — the UI only ever shows a couple of lines.
public static class RecordSummaries
{
    private const int MaxLength = 180;

    public static string? Clip(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        // Collapse whitespace/newlines so the summary reads as a single snippet.
        var flat = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (flat.Length <= MaxLength) return flat;

        // Cut on a word boundary where possible, then mark the truncation.
        var cut = flat.LastIndexOf(' ', MaxLength);
        if (cut < MaxLength / 2) cut = MaxLength;
        return flat[..cut].TrimEnd() + "…";
    }
}
