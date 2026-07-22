using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Subcontractors.Storage;
using Jewel.JPMS.Api.Gates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

/// <summary>
/// GET /api/subcontractors/{subcontractorId}/compliance/{documentId}/content — internal-side
/// download of a stored compliance file (any version, including superseded ones, for audit).
/// Same gate as the compliance list: internal roles read any record; a portal-scoped
/// subcontractor login only its own.
/// </summary>
public sealed class DownloadComplianceDocumentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IComplianceBlobStore blobStore;

    public DownloadComplianceDocumentEndpoint(
        SignedInUserResolver users, JpmsContext context, IComplianceBlobStore blobStore)
    {
        this.users = users;
        this.context = context;
        this.blobStore = blobStore;
    }

    private static readonly RoleSet InternalRolesThatMayReadCompliance = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator,
        JpmsRoles.SiteManager, JpmsRoles.HealthAndSafetyLead, JpmsRoles.OfficeComplianceCoordinator);

    [Function("DownloadComplianceDocument")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "subcontractors/{subcontractorId}/compliance/{documentId}/content")] HttpRequest request,
        string subcontractorId, string documentId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        if (!InternalRolesThatMayReadCompliance.IncludesAny(signedInUser.Roles))
        {
            var ownSubcontractorId = SubcontractorScope.OwnSubcontractorId(signedInUser);
            if (ownSubcontractorId is null
                || !string.Equals(ownSubcontractorId, subcontractorId, StringComparison.OrdinalIgnoreCase))
                return new StatusCodeResult(403);
        }

        var document = await context.ComplianceDocuments
            .FirstOrDefaultAsync(row => row.ComplianceDocumentId == documentId, cancellationToken);
        if (document is null || !string.Equals(document.SubcontractorId, subcontractorId, StringComparison.OrdinalIgnoreCase))
            return new NotFoundObjectResult("Document not found.");
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
