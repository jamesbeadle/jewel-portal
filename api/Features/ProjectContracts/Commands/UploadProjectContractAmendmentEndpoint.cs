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
/// POST /api/projects/{projectId}/contract/amendments — multipart/form-data, field name "file",
/// with optional "title", "amendmentDate" (yyyy-MM-dd) and "notes" fields alongside it.
///
/// <para>Blob first, row second, same as the executed contract upload: if storage fails no row is
/// written, so there is never an orphan row pointing at a file that does not exist. The reverse
/// (an orphan blob) is harmless.</para>
/// </summary>
public sealed class UploadProjectContractAmendmentEndpoint
{
    // Matches the executed-contract upload cap. A deed of variation is a document, not a drawing set.
    private const long MaxUploadBytes = 100L * 1024 * 1024;

    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IProjectContractBlobStore blobStore;
    private readonly AttachProjectContractAmendmentAuthorisation authorisation;
    private readonly AttachProjectContractAmendmentValidation validation;
    private readonly ICommandHandler<AttachProjectContractAmendment, ProjectContractAmendment> handler;

    public UploadProjectContractAmendmentEndpoint(
        SignedInUserResolver users,
        JpmsContext context,
        IProjectContractBlobStore blobStore,
        AttachProjectContractAmendmentAuthorisation authorisation,
        AttachProjectContractAmendmentValidation validation,
        ICommandHandler<AttachProjectContractAmendment, ProjectContractAmendment> handler)
    {
        this.users = users;
        this.context = context;
        this.blobStore = blobStore;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    // Literal rather than nameof: the command this endpoint builds is AttachProjectContractAmendment,
    // which is server-constructed. Same convention as UploadProjectContractDocument.
    [Function("UploadProjectContractAmendment")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/contract/amendments")] HttpRequest request,
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

        // Clamp to the column widths so an over-long browser filename cannot fail the row insert
        // after the blob is already stored.
        var fileName = Path.GetFileName(string.IsNullOrWhiteSpace(file.FileName) ? "amendment" : file.FileName);
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "amendment";
        if (fileName.Length > 256) fileName = fileName[^256..];
        var contentType = string.IsNullOrWhiteSpace(file.ContentType) || file.ContentType.Length > 128
            ? "application/octet-stream"
            : file.ContentType;

        // Falls back to the filename so the list is never a column of blanks — a title is how an
        // amendment reads. Clamped like the filename.
        var title = form.TryGetValue("title", out var titleValue) ? titleValue.ToString().Trim() : "";
        if (string.IsNullOrWhiteSpace(title)) title = fileName;
        if (title.Length > 256) title = title[..256];

        DateTimeOffset? amendmentDate = null;
        if (form.TryGetValue("amendmentDate", out var dateValue) && !string.IsNullOrWhiteSpace(dateValue))
        {
            if (!DateTimeOffset.TryParse(dateValue, out var parsed))
                return new BadRequestObjectResult("The amendment date could not be read — send it as yyyy-MM-dd.");
            amendmentDate = parsed;
        }

        var notes = form.TryGetValue("notes", out var notesValue) ? notesValue.ToString().Trim() : "";
        if (notes.Length > 4000) notes = notes[..4000];

        var projectContractAmendmentId = ProjectContractsIdentifierFactory.NextProjectContractAmendmentId();

        string blobRef;
        try
        {
            await using var stream = file.OpenReadStream();
            blobRef = await blobStore.UploadAmendmentAsync(
                projectId, projectContractAmendmentId, fileName, contentType, stream, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ObjectResult(
                $"Could not store the amendment document — check the contract storage configuration. ({ex.Message})")
            {
                StatusCode = StatusCodes.Status502BadGateway
            };
        }

        var command = new AttachProjectContractAmendment(
            projectId, projectContractAmendmentId, blobRef, fileName, contentType, file.Length,
            title, amendmentDate, string.IsNullOrWhiteSpace(notes) ? null : notes, signedInUser.Email);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
