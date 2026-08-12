using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.DocumentControl.Storage;
using Jewel.JPMS.Api.Gates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.DocumentControl.Queries;

/// <summary>
/// GET /api/document-control/items/{itemId}/file — streams a queue item's stored file. The
/// container is private, so the file is proxied through the API (never a public URL). ?inline=1
/// leaves Content-Disposition unset for the in-app viewer; otherwise the filename forces a
/// download (mirrors DownloadDrawingRevisionFileEndpoint).
/// </summary>
public sealed class DownloadDocumentControlFileEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IDocumentControlBlobStore blobStore;

    public DownloadDocumentControlFileEndpoint(
        SignedInUserResolver users, JpmsContext context, IDocumentControlBlobStore blobStore)
    {
        this.users = users;
        this.context = context;
        this.blobStore = blobStore;
    }

    [Function(nameof(DownloadDocumentControlFileEndpoint))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "document-control/items/{itemId}/file")] HttpRequest request,
        string itemId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!DocumentControlRoles.AllowedToManage.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var item = await context.DocumentControlItems.AsNoTracking()
            .FirstOrDefaultAsync(row => row.DocumentControlItemId == itemId, cancellationToken);
        if (item is null || string.IsNullOrWhiteSpace(item.BlobRef))
            return new NotFoundObjectResult("No file is stored for this document.");

        var blob = await blobStore.OpenAsync(item.BlobRef, cancellationToken);
        if (blob is null) return new NotFoundObjectResult("The stored file could not be found.");

        var inline = request.Query.TryGetValue("inline", out var inlineValue)
            && (inlineValue == "1" || string.Equals(inlineValue, "true", StringComparison.OrdinalIgnoreCase));

        // Range processing lets browser PDF viewers seek; omitting FileDownloadName renders inline.
        var result = new FileStreamResult(blob.Content, string.IsNullOrWhiteSpace(item.ContentType) ? blob.ContentType : item.ContentType)
        {
            EnableRangeProcessing = true
        };
        if (!inline)
            result.FileDownloadName = string.IsNullOrWhiteSpace(item.FileName) ? itemId : item.FileName;
        return result;
    }
}
