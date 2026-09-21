using Jewel.JPMS.Api.Features.MailboxIntake;

namespace Jewel.JPMS.Api.Storage;

/// <summary>
/// Whether a stored file may be shown in the browser rather than downloaded. A file served
/// without a Content-Disposition renders on the portal's own origin, so only content a browser
/// shows without executing anything is ever rendered inline — the embeddable raster types and
/// PDF. Everything else (an HTML or SVG upload above all) always downloads, whatever the caller
/// asked for and whatever type the upload declared; and every file response says nosniff so the
/// browser never guesses a type the portal did not send.
/// </summary>
public static class InlineRendering
{
    private const string PortableDocument = "application/pdf";
    private const string InlineQuery = "inline";

    public static bool IsAskedFor(HttpRequest request)
    {
        if (!request.Query.TryGetValue(InlineQuery, out var value)) return false;
        return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    public static bool MaySafelyRender(string? contentType)
    {
        var mediaType = MediaTypeOf(contentType);
        return InboundEmailBodyBuilder.EmbeddableImageTypes.Contains(mediaType)
            || string.Equals(mediaType, PortableDocument, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsInlineView(bool isAskedFor, string? contentType) =>
        isAskedFor && MaySafelyRender(contentType);

    public static void ForbidSniffing(HttpResponse response) =>
        response.Headers["X-Content-Type-Options"] = "nosniff";

    private static string MediaTypeOf(string? contentType)
    {
        var declared = contentType ?? "";
        var parameters = declared.IndexOf(';');
        var mediaType = parameters < 0 ? declared : declared[..parameters];
        return mediaType.Trim();
    }
}
