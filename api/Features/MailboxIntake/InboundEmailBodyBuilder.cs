using System.Net;
using System.Text.RegularExpressions;
using Ganss.Xss;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.MailboxIntake;

/// <summary>
/// Turns an inbound email's raw HTML into the safe body the portal renders, with pasted screenshots
/// showing where the sender put them — the inbound mirror of ComposeHtmlPipeline. SANITISE with the
/// cid: scheme kept, so inline image references survive the scrub; then EMBED — every surviving
/// &lt;img src="cid:…"&gt; whose image the message carries is rewritten to a data: URI built from
/// Graph's own attachment bytes. Embedding runs AFTER sanitisation, so a data: URL the sender wrote
/// into the body is still stripped — the only ones that reach a browser are built here. An image
/// over the caps, non-raster, or unfetchable keeps its cid: src — the same placeholder as before.</summary>
public sealed class InboundEmailBodyBuilder
{
    /// <summary>Per image — matches the compose pipeline's refusal limit for pasted images.</summary>
    public const long MaxEmbeddedImageBytes = 4_000_000;

    /// <summary>Per email — keeps one detail payload sane when a chain carries many screenshots.</summary>
    public const long MaxEmbeddedTotalBytes = 12_000_000;

    /// <summary>Raster formats only: an SVG can carry markup of its own, so it stays a placeholder.</summary>
    public static readonly IReadOnlySet<string> EmbeddableImageTypes = new HashSet<string>(
        new[] { "image/png", "image/jpeg", "image/jpg", "image/gif", "image/webp", "image/bmp" },
        StringComparer.OrdinalIgnoreCase);

    // Sanitisation re-serialises attributes double-quoted, so only that shape needs matching.
    private static readonly Regex CidImageSource = new(
        "src\\s*=\\s*\"cid:([^\"]+)\"",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IIntakeMessageReader reader;
    public InboundEmailBodyBuilder(IIntakeMessageReader reader) { this.reader = reader; }

    /// <summary>The sanitised body with its inline images embedded. Plain-text bodies pass through.</summary>
    public async Task<string> BuildAsync(string messageId, IntakeMessageContent content, CancellationToken cancellationToken)
    {
        if (!content.IsHtml) return content.Body;

        var sanitised = Sanitise(content.Body);
        var carried = content.InlineImages ?? Array.Empty<IntakeInlineImage>();
        if (carried.Count == 0) return sanitised;

        var referenced = CidImageSource.Matches(sanitised)
            .Select(match => NormaliseCid(match.Groups[1].Value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (referenced.Count == 0) return sanitised;

        var embedded = await FetchReferencedImagesAsync(messageId, referenced, carried, cancellationToken);
        if (embedded.Count == 0) return sanitised;

        return CidImageSource.Replace(sanitised, match =>
            embedded.TryGetValue(NormaliseCid(match.Groups[1].Value), out var dataUrl)
                ? $"src=\"{dataUrl}\""
                : match.Value);
    }

    // Graph's metadata read cannot say which cid an inline image answers to (contentId sits on the
    // fileAttachment subtype), so each carried image is fetched — within the caps — and matched to
    // the body's references by the contentId the full fetch returns.
    private async Task<Dictionary<string, string>> FetchReferencedImagesAsync(
        string messageId, IReadOnlySet<string> referenced, IReadOnlyList<IntakeInlineImage> carried,
        CancellationToken cancellationToken)
    {
        var embedded = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        long totalBytes = 0;
        foreach (var image in carried)
        {
            if (embedded.Count == referenced.Count) break;
            if (image.ContentType is { Length: > 0 } declaredType
                && !declaredType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) continue;
            if (image.Size > MaxEmbeddedImageBytes) continue;
            if (totalBytes + image.Size > MaxEmbeddedTotalBytes) continue;
            var file = await reader.GetAttachmentAsync(messageId, image.AttachmentId, cancellationToken);
            if (file?.ContentId is not { Length: > 0 } rawContentId) continue;
            var contentId = NormaliseCid(rawContentId);
            if (!referenced.Contains(contentId)) continue;
            if (!EmbeddableImageTypes.Contains(file.ContentType)) continue;
            if (file.Content.LongLength > MaxEmbeddedImageBytes) continue;
            totalBytes += file.Content.LongLength;
            embedded[contentId] = $"data:{file.ContentType};base64,{Convert.ToBase64String(file.Content)}";
        }
        return embedded;
    }

    // The sanitiser HTML-encodes attribute values, and the raw Content-ID header form wraps the
    // value in angle brackets, which Graph may pass through — so both sides of the match level here.
    private static string NormaliseCid(string value) =>
        WebUtility.HtmlDecode(value).Trim().TrimStart('<').TrimEnd('>');

    /// <summary>Ganss.Xss defaults plus the cid: scheme, so image references reach the embed step.</summary>
    public static string Sanitise(string html)
    {
        var sanitiser = new HtmlSanitizer();
        sanitiser.AllowedSchemes.Add("cid");
        return sanitiser.Sanitize(html);
    }
}
