using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.ProjectContracts.Storage;
using Jewel.JPMS.Api.Gates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Queries;

/// <summary>
/// GET /api/projects/{projectId}/contract/document — streams the executed contract.
///
/// <para>Pass <c>?inline=1</c> to omit Content-Disposition so the browser's PDF viewer renders it in
/// place; otherwise it downloads. Range processing is enabled either way, which is what lets the
/// viewer seek.</para>
/// </summary>
public sealed class DownloadProjectContractDocumentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IProjectContractBlobStore blobStore;

    public DownloadProjectContractDocumentEndpoint(
        SignedInUserResolver users,
        JpmsContext context,
        IProjectContractBlobStore blobStore)
    {
        this.users = users;
        this.context = context;
        this.blobStore = blobStore;
    }

    [Function("DownloadProjectContractDocument")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/contract/document")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ProjectContractRoles.AllowedToReadContract.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var contract = await context.ProjectContracts
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.ProjectId == projectId, cancellationToken);

        if (contract is null || string.IsNullOrWhiteSpace(contract.DocumentBlobRef))
            return new NotFoundObjectResult("No contract document has been uploaded for this project.");

        var blob = await blobStore.OpenAsync(contract.DocumentBlobRef, cancellationToken);
        if (blob is null) return new NotFoundObjectResult("The stored file could not be found.");

        var inline = request.Query.TryGetValue("inline", out var inlineValue)
            && (inlineValue == "1" || string.Equals(inlineValue, "true", StringComparison.OrdinalIgnoreCase));

        var result = new FileStreamResult(
            blob.Content,
            string.IsNullOrWhiteSpace(contract.DocumentContentType) ? blob.ContentType : contract.DocumentContentType)
        {
            EnableRangeProcessing = true
        };

        if (!inline)
        {
            result.FileDownloadName = string.IsNullOrWhiteSpace(contract.DocumentFileName)
                ? "contract"
                : contract.DocumentFileName;
        }

        return result;
    }
}
