using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.ProjectContracts.Storage;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ProjectContracts;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Commands;

/// <summary>
/// POST /api/projects/{projectId}/contract/document — multipart/form-data, field name "file".
///
/// <para>Blob first, row second: if storage fails no row is written, so there is never an orphan
/// row pointing at a file that does not exist. The reverse (an orphan blob) is harmless.</para>
/// </summary>
public sealed class UploadProjectContractDocumentEndpoint
{
    // Matches the compliance upload cap. An executed contract is a document, not a drawing set.
    private const long MaxUploadBytes = 100L * 1024 * 1024;

    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IProjectContractBlobStore blobStore;
    private readonly AttachProjectContractDocumentAuthorisation authorisation;
    private readonly AttachProjectContractDocumentValidation validation;
    private readonly ICommandHandler<AttachProjectContractDocument, ProjectContract> handler;

    public UploadProjectContractDocumentEndpoint(
        SignedInUserResolver users,
        JpmsContext context,
        IProjectContractBlobStore blobStore,
        AttachProjectContractDocumentAuthorisation authorisation,
        AttachProjectContractDocumentValidation validation,
        ICommandHandler<AttachProjectContractDocument, ProjectContract> handler)
    {
        this.users = users;
        this.context = context;
        this.blobStore = blobStore;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    // Literal rather than nameof: the command this endpoint builds is AttachProjectContractDocument,
    // which is server-constructed. Same convention as the download endpoints.
    [Function("UploadProjectContractDocument")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/contract/document")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!authorisation.Allows(signedInUser)) return new StatusCodeResult(403);

        if (!request.HasFormContentType) return new BadRequestObjectResult("Expected multipart/form-data.");
        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (file is null || file.Length == 0) return new BadRequestObjectResult("A non-empty file is required.");
        if (file.Length > MaxUploadBytes) return new BadRequestObjectResult("The file is too large (100 MB max).");

        var projectExists = await context.Projects
            .AnyAsync(row => row.ProjectId == projectId, cancellationToken);
        if (!projectExists) return new NotFoundObjectResult($"Project {projectId} not found.");

        // Reuse the existing contract id where there is one, so replacing the document does not
        // scatter blobs across folders for the same contract.
        var existingContractId = await context.ProjectContracts
            .Where(row => row.ProjectId == projectId)
            .Select(row => row.ProjectContractId)
            .FirstOrDefaultAsync(cancellationToken);
        var projectContractId = string.IsNullOrWhiteSpace(existingContractId)
            ? ProjectContractsIdentifierFactory.NextProjectContractId()
            : existingContractId;

        // Clamp to the column widths so an over-long browser filename cannot fail the row insert
        // after the blob is already stored.
        var fileName = Path.GetFileName(string.IsNullOrWhiteSpace(file.FileName) ? "contract" : file.FileName);
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "contract";
        if (fileName.Length > 256) fileName = fileName[^256..];
        var contentType = string.IsNullOrWhiteSpace(file.ContentType) || file.ContentType.Length > 128
            ? "application/octet-stream"
            : file.ContentType;

        string blobRef;
        try
        {
            await using var stream = file.OpenReadStream();
            blobRef = await blobStore.UploadAsync(
                projectId, projectContractId, fileName, contentType, stream, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ObjectResult(
                $"Could not store the contract document — check the contract storage configuration. ({ex.Message})")
            {
                StatusCode = StatusCodes.Status502BadGateway
            };
        }

        var command = new AttachProjectContractDocument(
            projectId, blobRef, fileName, contentType, file.Length, signedInUser.Email);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
