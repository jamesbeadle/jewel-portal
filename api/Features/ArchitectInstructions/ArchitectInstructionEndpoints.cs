using Jewel.JPMS.Api.Storage;
using System.Globalization;
using Jewel.JPMS.Api.Features.ArchitectInstructions.Storage;
using Jewel.JPMS.Contracts.ArchitectInstructions;

namespace Jewel.JPMS.Api.Features.ArchitectInstructions;

/// <summary>
/// The writes on the Architect's Instruction register. The JSON commands follow the house four-part
/// shape; the upload is multipart, so it is a hand-written endpoint rather than routed through the
/// JSON command sender — exactly as drawings do it. The reads, which the architect's own login
/// shares, live in ArchitectInstructionReadEndpoints, away from the mailbox import.
/// </summary>
public sealed class ArchitectInstructionEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IArchitectInstructionBlobStore blobStore;
    private readonly ICommandHandler<RecordArchitectInstruction, ArchitectInstruction> record;
    private readonly ICommandHandler<ImportArchitectInstructionFromMessage, ArchitectInstruction> import;
    private readonly ICommandHandler<UpdateArchitectInstruction, ArchitectInstruction> update;
    private readonly ICommandHandler<LinkArchitectInstructionToVariation, ArchitectInstruction> link;
    private readonly ICommandHandler<UnlinkArchitectInstructionFromVariation, ArchitectInstruction> unlink;
    private readonly ICommandHandler<DeleteArchitectInstruction, Acknowledgement> delete;

    public ArchitectInstructionEndpoints(
        SignedInUserResolver users,
        JpmsContext context,
        IArchitectInstructionBlobStore blobStore,
        ICommandHandler<RecordArchitectInstruction, ArchitectInstruction> record,
        ICommandHandler<ImportArchitectInstructionFromMessage, ArchitectInstruction> import,
        ICommandHandler<UpdateArchitectInstruction, ArchitectInstruction> update,
        ICommandHandler<LinkArchitectInstructionToVariation, ArchitectInstruction> link,
        ICommandHandler<UnlinkArchitectInstructionFromVariation, ArchitectInstruction> unlink,
        ICommandHandler<DeleteArchitectInstruction, Acknowledgement> delete)
    {
        this.users = users;
        this.context = context;
        this.blobStore = blobStore;
        this.record = record;
        this.import = import;
        this.update = update;
        this.link = link;
        this.unlink = unlink;
        this.delete = delete;
    }

    /// <summary>
    /// POST /api/projects/{projectId}/architect-instructions — multipart/form-data.
    /// Form fields: optional <c>file</c> (the instruction document), <c>instructionRef</c>,
    /// <c>title</c>, optional <c>notes</c>, <c>instructedAt</c> (yyyy-MM-dd), <c>issuedByEmail</c>,
    /// and repeated <c>variationOrderId</c> values to link as it is filed.
    ///
    /// The file is optional on purpose: a PM who knows an instruction has been given, but has not
    /// been sent the paperwork yet, can open the row now and attach the document when it lands,
    /// rather than leaving the variation's evidence trail with a hole in it.
    /// </summary>
    [Function(nameof(RecordArchitectInstruction))]
    public async Task<IActionResult> Record(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/architect-instructions")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ArchitectInstructionRoles.AllowedToManage.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!await ArchitectInstructionScope.MayFileOnProjectAsync(context, signedInUser, projectId, cancellationToken))
            return new StatusCodeResult(403);

        if (!request.HasFormContentType) return new BadRequestObjectResult("Expected multipart/form-data.");
        var form = await request.ReadFormAsync(cancellationToken);

        var projectExists = await context.Projects.AnyAsync(row => row.ProjectId == projectId, cancellationToken);
        if (!projectExists) return new NotFoundObjectResult($"Project {projectId} not found.");

        var instructionRef = form["instructionRef"].ToString().Trim();
        var title = form["title"].ToString().Trim();
        var notes = form["notes"].ToString().Trim();
        var issuedByEmail = form["issuedByEmail"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(issuedByEmail)) issuedByEmail = signedInUser.Email;

        if (string.IsNullOrWhiteSpace(instructionRef) && string.IsNullOrWhiteSpace(title))
            return new BadRequestObjectResult("Give the instruction a reference or a title so it can be found again.");

        DateTimeOffset? instructedAt = null;
        var rawInstructedAt = form["instructedAt"].ToString().Trim();
        if (!string.IsNullOrWhiteSpace(rawInstructedAt))
        {
            if (!DateTimeOffset.TryParse(rawInstructedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
                return new BadRequestObjectResult("The instruction date could not be read — use yyyy-MM-dd.");
            instructedAt = parsed;
        }

        var variationOrderIds = form["variationOrderId"]
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToList();

        var instructionId = ArchitectInstructionIdentifierFactory.NextArchitectInstructionId();
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();

        string? blobRef = null, fileName = null, contentType = null;
        long? fileSizeBytes = null;
        if (file is not null && file.Length > 0)
        {
            fileName = string.IsNullOrWhiteSpace(file.FileName) ? "instruction" : file.FileName;
            contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
            fileSizeBytes = file.Length;
            try
            {
                await using var stream = file.OpenReadStream();
                blobRef = await blobStore.UploadAsync(
                    projectId, instructionId, fileName, contentType, stream, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Nothing has been written to the database yet, so there is no orphan row to clean up.
                return new ObjectResult($"Could not store the instruction document. ({ex.Message})")
                {
                    StatusCode = StatusCodes.Status502BadGateway
                };
            }
        }

        var command = new RecordArchitectInstruction(
            instructionId, projectId, instructionRef, title, notes, instructedAt,
            issuedByEmail, signedInUser.Email, ArchitectInstructionSource.Upload,
            fileName, contentType, fileSizeBytes, blobRef, variationOrderIds);

        return new OkObjectResult(await record.HandleAsync(command, cancellationToken));
    }

    [Function(nameof(ImportArchitectInstructionFromMessage))]
    public async Task<IActionResult> Import(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/architect-instructions/import-from-message")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ArchitectInstructionRoles.AllowedToImportFromMail.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!await ArchitectInstructionScope.MayFileOnProjectAsync(context, signedInUser, projectId, cancellationToken))
            return new StatusCodeResult(403);

        ImportArchitectInstructionFromMessage? body = null;
        try { body = await request.ReadFromJsonAsync<ImportArchitectInstructionFromMessage>(cancellationToken); }
        catch { /* reported as a validation failure below */ }
        if (body is null || string.IsNullOrWhiteSpace(body.MessageId) || string.IsNullOrWhiteSpace(body.AttachmentId))
            return new BadRequestObjectResult("The email and the attachment to import are both required.");

        try
        {
            var command = body with { ProjectId = projectId };
            return new OkObjectResult(await import.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }

    [Function(nameof(UpdateArchitectInstruction))]
    public async Task<IActionResult> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "architect-instructions/{instructionId}")] HttpRequest request,
        string instructionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ArchitectInstructionRoles.AllowedToManage.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!await ArchitectInstructionScope.MayActOnAsync(context, signedInUser, instructionId, cancellationToken))
            return new StatusCodeResult(403);

        UpdateArchitectInstruction? body = null;
        try { body = await request.ReadFromJsonAsync<UpdateArchitectInstruction>(cancellationToken); }
        catch { }
        if (body is null) return new BadRequestObjectResult("A body is required.");
        if (string.IsNullOrWhiteSpace(body.InstructionRef) && string.IsNullOrWhiteSpace(body.Title))
            return new BadRequestObjectResult("Give the instruction a reference or a title so it can be found again.");

        try
        {
            return new OkObjectResult(
                await update.HandleAsync(body with { ArchitectInstructionId = instructionId }, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }

    [Function(nameof(LinkArchitectInstructionToVariation))]
    public async Task<IActionResult> Link(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "architect-instructions/{instructionId}/variations/{variationOrderId}")] HttpRequest request,
        string instructionId, string variationOrderId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ArchitectInstructionRoles.AllowedToManage.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!await ArchitectInstructionScope.MayActOnAsync(context, signedInUser, instructionId, cancellationToken))
            return new StatusCodeResult(403);

        try
        {
            return new OkObjectResult(await link.HandleAsync(
                new LinkArchitectInstructionToVariation(instructionId, variationOrderId), cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }

    [Function(nameof(UnlinkArchitectInstructionFromVariation))]
    public async Task<IActionResult> Unlink(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "architect-instructions/{instructionId}/variations/{variationOrderId}")] HttpRequest request,
        string instructionId, string variationOrderId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ArchitectInstructionRoles.AllowedToManage.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!await ArchitectInstructionScope.MayActOnAsync(context, signedInUser, instructionId, cancellationToken))
            return new StatusCodeResult(403);

        try
        {
            return new OkObjectResult(await unlink.HandleAsync(
                new UnlinkArchitectInstructionFromVariation(instructionId, variationOrderId), cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }

    [Function(nameof(DeleteArchitectInstruction))]
    public async Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "architect-instructions/{instructionId}")] HttpRequest request,
        string instructionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ArchitectInstructionRoles.AllowedToManage.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!await ArchitectInstructionScope.MayActOnAsync(context, signedInUser, instructionId, cancellationToken))
            return new StatusCodeResult(403);

        return new OkObjectResult(
            await delete.HandleAsync(new DeleteArchitectInstruction(instructionId), cancellationToken));
    }
}
