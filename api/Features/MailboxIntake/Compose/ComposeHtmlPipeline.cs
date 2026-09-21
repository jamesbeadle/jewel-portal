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

    public ComposedBody FromTypedHtml(string bodyHtml) =>
        WithPastedImagesLifted(TypedBodySanitiser().Sanitize(bodyHtml ?? ""));

    /// <summary>A typed body kept as a DRAFT: the typed rule, with pasted images left in place so
    /// the composer can show them again. A draft is rendered back to whoever opens the composer
    /// next, so it is cleaned on the way in, not only on the way out (2026-09-21).</summary>
    public static string AsDraft(string? bodyHtml) =>
        string.IsNullOrWhiteSpace(bodyHtml) ? bodyHtml ?? "" : TypedBodySanitiser().Sanitize(bodyHtml);

    /// <summary>HTML the portal composed itself, cleaned without losing how it looks. Whitespace in
    /// means whitespace out: an empty or blank body is left exactly as it arrived, so a caller that
    /// meant "no cover note" still sends none.</summary>
    public static string FromPortalDocument(string? bodyHtml) =>
        string.IsNullOrWhiteSpace(bodyHtml) ? bodyHtml ?? "" : PortalDocumentSanitiser().Sanitize(bodyHtml);

    /// <summary>What the dispatcher stages: the portal rule, plus the pasted images lifted out of
    /// the body. Every compose surface is a rich editor since 2026-09-18, so a screenshot pasted
    /// into a purchase order or a statement arrives as a data: URL — which many mail clients strip
    /// or refuse. Server-composed bodies carry none, so for them this is the sanitising alone.</summary>
    public static ComposedBody ForStaging(string? bodyHtml) =>
        string.IsNullOrWhiteSpace(bodyHtml)
            ? new ComposedBody(bodyHtml ?? "", Array.Empty<MailboxDraftAttachment>())
            : WithPastedImagesLifted(FromPortalDocument(bodyHtml));
}
