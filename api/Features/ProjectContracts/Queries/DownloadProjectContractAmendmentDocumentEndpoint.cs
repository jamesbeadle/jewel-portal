using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.ProjectContracts.Storage;
using Jewel.JPMS.Api.Gates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Queries;

/// <summary>
/// GET /api/projects/{projectId}/contract/amendments/{amendmentId}/document — streams one
/// amendment's document.
///
/// <para>Pass <c>?inline=1</c> to omit Content-Disposition so the browser's PDF viewer renders it
/// in place; otherwise it downloads. Range processing is enabled either way, which is what lets
/// the viewer seek. Same contract as the executed-contract download.</para>
/// </summary>
public sealed class DownloadProjectContractAmendmentDocumentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IProjectContractBlobStore blobStore;

    public DownloadProjectContractAmendmentDocumentEndpoint(
        SignedInUserResolver users,
        JpmsContext context,
        IProjectContractBlobStore blobStore)
    {
        this.users = users;
        this.context = context;
        this.blobStore = blobStore;
    }

    [Function("DownloadProjectContractAmendmentDocument")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/contract/amendments/{amendmentId}/document")] HttpRequest request,
        string projectId,
        string amendmentId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ProjectContractRoles.AllowedToReadContract.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var amendment = await context.ProjectContractAmendments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                row => row.ProjectContractAmendmentId == amendmentId && row.ProjectId == projectId,
                cancellationToken);

        if (amendment is null || string.IsNullOrWhiteSpace(amendment.DocumentBlobRef))
            return new NotFoundObjectResult("That amendment no longer exists — it may have been removed.");

        var blob = await blobStore.OpenAsync(amendment.DocumentBlobRef, cancellationToken);
        if (blob is null) return new NotFoundObjectResult("The stored file could not be found.");

        var inline = request.Query.TryGetValue("inline", out var inlineValue)
            && (inlineValue == "1" || string.Equals(inlineValue, "true", StringComparison.OrdinalIgnoreCase));

        var result = new FileStreamResult(
            blob.Content,
            string.IsNullOrWhiteSpace(amendment.DocumentContentType) ? blob.ContentType : amendment.DocumentContentType)
        {
            EnableRangeProcessing = true
        };

        if (!inline)
        {
            result.FileDownloadName = string.IsNullOrWhiteSpace(amendment.DocumentFileName)
                ? "amendment"
                : amendment.DocumentFileName;
        }

        return result;
    }
}
