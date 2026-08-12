using System.Text.RegularExpressions;
using Ganss.Xss;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

/// <summary>
/// Turns the composer's body into safe outbound draft HTML.
///
/// Two jobs, in order:
///   1. SANITISE — the body arrives from a contenteditable surface (and pasted content can carry
///      anything), so it is reduced to a small outbound allowlist before it goes anywhere near a
///      draft. The same Ganss.Xss sanitiser the inbound reads use, configured tighter: basic text
///      structure, links, and images whose src is data: (a pasted image) or cid: (already inline).
///   2. EXTRACT PASTED IMAGES — every <img src="data:image/…;base64,…"> becomes a proper inline
///      fileAttachment with a ContentId, and the src is rewritten to cid:{id}, because mail clients
///      render cid images reliably while multi-megabyte data: URLs are stripped or refused by many
///      of them (and bloat the stored message).
///
/// Plain-text bodies (BodyIsHtml = false) skip all of this via <see cref="FromPlainText"/> — the
/// textarea's text is HTML-encoded line by line, exactly as the old reply flow did.
/// </summary>
public sealed class ComposeHtmlPipeline
{
    /// <summary>A pasted image larger than this is refused (the composer should have downscaled or
    /// attached it as a file instead) — 4 MB of real bytes, well under Graph's inline limit.</summary>
    public const long MaxInlineImageBytes = 4_000_000;

    private static readonly Regex DataImage = new(
        "src\\s*=\\s*\"data:(image/[a-zA-Z0-9.+-]+);base64,([A-Za-z0-9+/=\\s]+)\"",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Sanitised HTML plus the inline attachments extracted from it.</summary>
    public sealed record ComposedBody(string Html, IReadOnlyList<MailboxDraftAttachment> InlineImages);

    public ComposedBody FromHtml(string bodyHtml)
    {
        var sanitised = Sanitiser().Sanitize(bodyHtml ?? "");

        var inline = new List<MailboxDraftAttachment>();
        var index = 0;
        var rewritten = DataImage.Replace(sanitised, match =>
        {
            var contentType = match.Groups[1].Value;
            byte[] bytes;
            try { bytes = Convert.FromBase64String(match.Groups[2].Value.Trim()); }
            catch (FormatException)
            {
                throw new InvalidOperationException("A pasted image couldn't be read — remove it and paste it again.");
            }
            if (bytes.LongLength > MaxInlineImageBytes)
                throw new InvalidOperationException(
                    "A pasted image is larger than 4 MB — attach it as a file instead of pasting it into the body.");

            index++;
            var contentId = $"pasted-{Guid.NewGuid():N}";
            var extension = contentType.Split('/').Last() switch
            {
                "jpeg" => "jpg",
                var e when e.Length is > 0 and <= 8 => e,
                _ => "png"
            };
            inline.Add(new MailboxDraftAttachment(
                $"pasted-image-{index}.{extension}", contentType, bytes, IsInline: true, ContentId: contentId));
            return $"src=\"cid:{contentId}\"";
        });

        return new ComposedBody(rewritten, inline);
    }

    /// <summary>Plain textarea text → draft HTML: encode each line, join with &lt;br&gt;, and leave a
    /// blank line before any quoted history the result is prepended to.</summary>
    public static string FromPlainText(string body) =>
        "<div>"
        + string.Join("<br>", (body ?? "").Replace("\r\n", "\n").Split('\n').Select(System.Net.WebUtility.HtmlEncode))
        + "</div><br>";

    private static HtmlSanitizer Sanitiser()
    {
        var sanitiser = new HtmlSanitizer();
        sanitiser.AllowedTags.Clear();
        foreach (var tag in new[]
        {
            "p", "div", "br", "b", "strong", "i", "em", "u", "s",
            "ul", "ol", "li", "a", "blockquote", "span", "pre", "code", "img",
            "h1", "h2", "h3", "h4", "table", "thead", "tbody", "tr", "th", "td", "hr"
        })
            sanitiser.AllowedTags.Add(tag);

        sanitiser.AllowedAttributes.Clear();
        sanitiser.AllowedAttributes.Add("href");
        sanitiser.AllowedAttributes.Add("src");
        sanitiser.AllowedAttributes.Add("alt");
        sanitiser.AllowedAttributes.Add("style"); // colour only — see AllowedCssProperties below

        // Style pass-through is a single property: color, for the composer's text-colour button
        // (which writes <span style="color:…">). Everything else in a style attribute — the
        // classic sanitiser escape hatch — is still stripped, so outbound mail otherwise carries
        // the recipient's default styling.
        sanitiser.AllowedCssProperties.Clear();
        sanitiser.AllowedCssProperties.Add("color");
        sanitiser.AllowedAtRules.Clear();

        sanitiser.AllowedSchemes.Clear();
        sanitiser.AllowedSchemes.Add("http");
        sanitiser.AllowedSchemes.Add("https");
        sanitiser.AllowedSchemes.Add("mailto");
        sanitiser.AllowedSchemes.Add("data"); // pasted images — extracted to cid right after
        sanitiser.AllowedSchemes.Add("cid");
        return sanitiser;
    }
}
