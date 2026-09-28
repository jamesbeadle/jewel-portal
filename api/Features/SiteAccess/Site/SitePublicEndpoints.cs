using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Api.Storage;

namespace Jewel.JPMS.Api.Features.SiteAccess.Site;

/// <summary>
/// THE anonymous surface of the drawing system, in one class as the imagine and public-form doors
/// are. GET /api/site/{token} is what a QR poster opens: the folder's current drawings as one plain
/// mobile page, counted as a scan. GET /api/site/{token}/file/{revisionId} streams one drawing
/// exactly as the signed-in download does, after the containment check — the revision must belong
/// to a drawing in the link's own folders on the link's own project — and an inline view (the
/// page's own taps) never inflates the revision's view count. A token that resolves to nothing, a
/// revision outside the link and a missing file all answer the same plain 404.
/// </summary>
public sealed class SitePublicEndpoints
{
    private const string HtmlContentType = "text/html; charset=utf-8";

    private readonly SiteLinkResolver resolver;
    private readonly SiteDrawingListing listing;
    private readonly SiteLinkScans scans;
    private readonly SiteDrawingReach reach;
    private readonly IDrawingBlobStore blobStore;
    private readonly JpmsContext context;

    public SitePublicEndpoints(
        SiteLinkResolver resolver, SiteDrawingListing listing, SiteLinkScans scans,
        SiteDrawingReach reach, IDrawingBlobStore blobStore, JpmsContext context)
    {
        this.resolver = resolver;
        this.listing = listing;
        this.scans = scans;
        this.reach = reach;
        this.blobStore = blobStore;
        this.context = context;
    }

    [Function("SiteDrawingsPage")]
    public async Task<IActionResult> OpenPage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "site/{token}")] HttpRequest request,
        string token)
    {
        var httpContext = request.HttpContext;
        var cancellationToken = httpContext.RequestAborted;
        SiteResponseHeaders.Apply(httpContext.Response);

        var link = await resolver.ResolveAsync(token, cancellationToken);
        if (link is null) return resolver.Refuse(request);

        var page = await listing.ReadAsync(link, cancellationToken);
        await scans.RecordAsync(link, cancellationToken);
        return new ContentResult
        {
            Content = SiteDrawingsPageHtml.Render(page, token),
            ContentType = HtmlContentType,
            StatusCode = StatusCodes.Status200OK
        };
    }

    [Function("SiteDrawingFile")]
    public async Task<IActionResult> OpenFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "site/{token}/file/{revisionId}")] HttpRequest request,
        string token,
        string revisionId)
    {
        var httpContext = request.HttpContext;
        var cancellationToken = httpContext.RequestAborted;
        SiteResponseHeaders.Apply(httpContext.Response);

        var link = await resolver.ResolveAsync(token, cancellationToken);
        if (link is null) return resolver.Refuse(request);
        var revision = await reach.RevisionWithinAsync(link, revisionId, cancellationToken);
        if (revision is null) return resolver.Refuse(request);
        var blob = await blobStore.OpenAsync(revision.BlobRef!, cancellationToken);
        if (blob is null) return resolver.Refuse(request);

        await CountViewUnlessInline(request, revision, cancellationToken);
        return StreamOf(blob, revision, request, revisionId);
    }

    private async Task CountViewUnlessInline(HttpRequest request, DrawingRevisionEntity revision, CancellationToken cancellationToken)
    {
        if (InlineRendering.IsAskedFor(request)) return;
        revision.ViewCount += 1;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static FileStreamResult StreamOf(DrawingBlob blob, DrawingRevisionEntity revision, HttpRequest request, string revisionId)
    {
        var isInline = InlineRendering.IsAskedFor(request);
        var result = new FileStreamResult(blob.Content, revision.ContentType ?? blob.ContentType) { EnableRangeProcessing = true };
        if (!InlineRendering.IsInlineView(isInline, result.ContentType))
            result.FileDownloadName = string.IsNullOrWhiteSpace(revision.FileName) ? revisionId : revision.FileName;
        return result;
    }
}
