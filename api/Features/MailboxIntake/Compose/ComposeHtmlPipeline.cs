using System.Text.RegularExpressions;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

/// <summary>
/// Turns a body into safe outbound draft HTML. There are TWO rules here, because a body a person
/// typed and a body the portal wrote are not the same risk (2026-09-18):
///
///   <see cref="FromTypedHtml"/> — a contenteditable surface, so anything could have been pasted
///   into it. Reduced to a small allowlist, and its only style pass-through is <c>color</c>, for
///   the composer's text-colour button. It also EXTRACTS PASTED IMAGES: every
///   <![CDATA[<img src="data:image/…;base64,…">]]> becomes an inline fileAttachment with a
///   ContentId and the src is rewritten to cid:{id}, because mail clients render cid images
///   reliably while multi-megabyte data: URLs are stripped or refused by many of them.
///
///   <see cref="FromPortalDocument"/> — HTML this codebase composed: a request cover note, a
///   purchase-order table, a statement. It keeps the inline styles and table attributes that
///   branded mail lives by, and still strips scripts, event handlers and dangerous schemes.
///   Sanitising these with the typed rule would have been one line and would have made every RFI,
///   purchase order and statement arrive as unstyled text.
///
/// The dispatcher runs every outbound body through <see cref="FromPortalDocument"/> as a floor, so
/// no door can send a script whatever it hands over; a door whose body a person typed runs the
/// tighter rule first. Plain-text bodies skip both via <see cref="FromPlainText"/>.
/// </summary>
public sealed partial class ComposeHtmlPipeline
{
    /// <summary>A pasted image larger than this is refused (the composer should have downscaled or
    /// attached it as a file instead) — 4 MB of real bytes, well under Graph's inline limit.</summary>
    public const long MaxInlineImageBytes = 4_000_000;

    private static readonly Regex DataImage = new(
        "src\\s*=\\s*\"data:(image/[a-zA-Z0-9.+-]+);base64,([A-Za-z0-9+/=\\s]+)\"",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Sanitised HTML plus the inline attachments extracted from it.</summary>
    public sealed record ComposedBody(string Html, IReadOnlyList<MailboxDraftAttachment> InlineImages);

    public ComposedBody FromTypedHtml(string bodyHtml)
    {
        var sanitised = TypedBodySanitiser().Sanitize(bodyHtml ?? "");

        var inline = new List<MailboxDraftAttachment>();
        var index = 0;
        var rewritten = DataImage.Replace(sanitised, match =>
        {
            index++;
            var attachment = InlineImage(match, index);
            inline.Add(attachment);
            return $"src=\"cid:{attachment.ContentId}\"";
        });

        return new ComposedBody(rewritten, inline);
    }

    /// <summary>HTML the portal composed itself, cleaned without losing how it looks. Whitespace in
    /// means whitespace out: an empty or blank body is left exactly as it arrived, so a caller that
    /// meant "no cover note" still sends none.</summary>
    public static string FromPortalDocument(string? bodyHtml) =>
        string.IsNullOrWhiteSpace(bodyHtml) ? bodyHtml ?? "" : PortalDocumentSanitiser().Sanitize(bodyHtml);

    /// <summary>Plain textarea text → draft HTML: encode each line, join with &lt;br&gt;, and leave a
    /// blank line before any quoted history the result is prepended to.</summary>
    public static string FromPlainText(string body) =>
        "<div>"
        + string.Join("<br>", (body ?? "").Replace("\r\n", "\n").Split('\n').Select(System.Net.WebUtility.HtmlEncode))
        + "</div><br>";

    private static MailboxDraftAttachment InlineImage(Match match, int index)
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

        var extension = contentType.Split('/').Last() switch
        {
            "jpeg" => "jpg",
            var suffix when suffix.Length is > 0 and <= 8 => suffix,
            _ => "png"
        };
        return new MailboxDraftAttachment(
            $"pasted-image-{index}.{extension}", contentType, bytes,
            IsInline: true, ContentId: $"pasted-{Guid.NewGuid():N}");
    }
}
