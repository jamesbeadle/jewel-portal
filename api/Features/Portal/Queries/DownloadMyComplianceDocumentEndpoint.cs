using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Subcontractors.Storage;
using Jewel.JPMS.Api.Gates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Portal.Queries;

/// <summary>
/// GET /api/portal/my/documents/{documentId}/content — streams a stored compliance file back to
/// the signed-in subcontractor. The document must belong to the caller's own record
/// (SubcontractorScope); the container is private so files are always proxied through the API.
/// </summary>
public sealed class DownloadMyComplianceDocumentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IComplianceBlobStore blobStore;

    public DownloadMyComplianceDocumentEndpoint(
        SignedInUserResolver users, JpmsContext context, IComplianceBlobStore blobStore)
    {
        this.users = users;
        this.context = context;
        this.blobStore = blobStore;
    }

    [Function("DownloadMyComplianceDocument")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "portal/my/documents/{documentId}/content")] HttpRequest request,
        string documentId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var subcontractorId = SubcontractorScope.OwnSubcontractorId(signedInUser);
        if (subcontractorId is null) return new ForbidResult();

        var document = await context.ComplianceDocuments
            .FirstOrDefaultAsync(row => row.ComplianceDocumentId == documentId, cancellationToken);
        if (document is null || !string.Equals(document.SubcontractorId, subcontractorId, StringComparison.OrdinalIgnoreCase))
            return new NotFoundObjectResult("Document not found."); // Not Forbid: don't reveal other companies' ids.
        if (string.IsNullOrWhiteSpace(document.BlobPath))
            return new NotFoundObjectResult("No file is stored for this document.");

        var blob = await blobStore.OpenAsync(document.BlobPath, cancellationToken);
        if (blob is null) return new NotFoundObjectResult("The stored file could not be found.");

        return new FileStreamResult(blob.Content, string.IsNullOrWhiteSpace(document.ContentType) ? blob.ContentType : document.ContentType)
        {
            FileDownloadName = string.IsNullOrWhiteSpace(document.FileName) ? documentId : document.FileName,
            EnableRangeProcessing = true
        };
    }
}
