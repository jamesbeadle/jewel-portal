using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Api.Gates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Drawings.Queries;

/// <summary>
/// GET /api/drawings/revisions/{revisionId}/file — streams the stored file for a revision.
/// The container is private, so the file is proxied through the API (never a public URL). Each
/// successful download bumps the revision's view count.
/// </summary>
public sealed class DownloadDrawingRevisionFileEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IDrawingBlobStore blobStore;

    public DownloadDrawingRevisionFileEndpoint(
        SignedInUserResolver users,
        JpmsContext context,
        IDrawingBlobStore blobStore)
    {
        this.users = users;
        this.context = context;
        this.blobStore = blobStore;
    }

    [Function(nameof(DownloadDrawingRevisionFileEndpoint))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "drawings/revisions/{revisionId}/file")] HttpRequest request,
        string revisionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var revision = await context.DrawingRevisions
            .FirstOrDefaultAsync(row => row.DrawingRevisionId == revisionId, cancellationToken);
        if (revision is null || string.IsNullOrWhiteSpace(revision.BlobRef))
            return new NotFoundObjectResult("No file is stored for this revision.");

        var blob = await blobStore.OpenAsync(revision.BlobRef, cancellationToken);
        if (blob is null) return new NotFoundObjectResult("The stored file could not be found.");

        // Inline requests (?inline=1) are for the in-app viewer, which re-requests the
        // file whenever the page re-renders — so they must not inflate the view count.
        var inline = request.Query.TryGetValue("inline", out var inlineValue)
            && (inlineValue == "1" || string.Equals(inlineValue, "true", StringComparison.OrdinalIgnoreCase));

        if (!inline)
        {
            revision.ViewCount += 1;
            await context.SaveChangesAsync(cancellationToken);
        }

        // Range processing lets browser PDF viewers seek. Omitting FileDownloadName leaves
        // Content-Disposition unset, so the browser renders the file inline in the iframe;
        // for explicit downloads we set the filename to force the attachment behaviour.
        var result = new FileStreamResult(blob.Content, revision.ContentType ?? blob.ContentType)
        {
            EnableRangeProcessing = true
        };
        if (!inline)
            result.FileDownloadName = string.IsNullOrWhiteSpace(revision.FileName) ? $"{revisionId}" : revision.FileName;
        return result;
    }
}
