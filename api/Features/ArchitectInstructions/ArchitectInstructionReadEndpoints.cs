using Jewel.JPMS.Api.Storage;
using Jewel.JPMS.Api.Features.ArchitectInstructions.Storage;
using Jewel.JPMS.Contracts.ArchitectInstructions;

namespace Jewel.JPMS.Api.Features.ArchitectInstructions;

/// <summary>
/// The reads on the Architect's Instruction register — the list, one instruction and its document
/// (a private blob, proxied here rather than handed out as a URL). The architect who issues the
/// instructions reads them too, confined to the projects their login was given
/// (ArchitectInstructionScope); the reads stand apart from the writes so nothing here reaches
/// the mailbox import.
/// </summary>
public sealed class ArchitectInstructionReadEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IArchitectInstructionBlobStore blobStore;
    private readonly IQueryHandler<ListArchitectInstructionsForProject, IReadOnlyList<ArchitectInstruction>> list;
    private readonly IQueryHandler<GetArchitectInstructionById, ArchitectInstruction?> get;

    public ArchitectInstructionReadEndpoints(
        SignedInUserResolver users,
        JpmsContext context,
        IArchitectInstructionBlobStore blobStore,
        IQueryHandler<ListArchitectInstructionsForProject, IReadOnlyList<ArchitectInstruction>> list,
        IQueryHandler<GetArchitectInstructionById, ArchitectInstruction?> get)
    {
        this.users = users;
        this.context = context;
        this.blobStore = blobStore;
        this.list = list;
        this.get = get;
    }

    [Function(nameof(ListArchitectInstructionsForProject))]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/architect-instructions")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ArchitectInstructionRoles.AllowedToRead.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!await ArchitectInstructionScope.MayReadProjectAsync(context, signedInUser, projectId, cancellationToken))
            return new StatusCodeResult(403);

        return new OkObjectResult(
            await list.HandleAsync(new ListArchitectInstructionsForProject(projectId), cancellationToken));
    }

    [Function(nameof(GetArchitectInstructionById))]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "architect-instructions/{instructionId}")] HttpRequest request,
        string instructionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ArchitectInstructionRoles.AllowedToRead.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!await ArchitectInstructionScope.MayActOnAsync(context, signedInUser, instructionId, cancellationToken))
            return new StatusCodeResult(403);

        var instruction = await get.HandleAsync(new GetArchitectInstructionById(instructionId), cancellationToken);
        return instruction is null
            ? new NotFoundObjectResult($"Architect's Instruction {instructionId} not found.")
            : new OkObjectResult(instruction);
    }

    /// <summary>
    /// GET /api/architect-instructions/{instructionId}/file — streams the stored document. The
    /// container is private, so the file is proxied here rather than handed out as a URL.
    /// </summary>
    [Function(nameof(DownloadArchitectInstructionFile))]
    public async Task<IActionResult> DownloadArchitectInstructionFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "architect-instructions/{instructionId}/file")] HttpRequest request,
        string instructionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ArchitectInstructionRoles.AllowedToRead.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!await ArchitectInstructionScope.MayActOnAsync(context, signedInUser, instructionId, cancellationToken))
            return new StatusCodeResult(403);

        var entity = await context.ArchitectInstructions
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.ArchitectInstructionId == instructionId, cancellationToken);
        if (entity is null || string.IsNullOrWhiteSpace(entity.BlobRef))
            return new NotFoundObjectResult("No document is stored for this instruction.");

        var blob = await blobStore.OpenAsync(entity.BlobRef, cancellationToken);
        if (blob is null) return new NotFoundObjectResult("The stored document could not be found.");

        // ?inline=1 renders in the in-app viewer; anything else downloads with its filename.
        var inline = InlineRendering.IsAskedFor(request);
        InlineRendering.ForbidSniffing(request.HttpContext.Response);

        var result = new FileStreamResult(blob.Content, entity.ContentType ?? blob.ContentType)
        {
            EnableRangeProcessing = true
        };
        if (!InlineRendering.IsInlineView(inline, result.ContentType))
            result.FileDownloadName = string.IsNullOrWhiteSpace(entity.FileName) ? $"{entity.Reference}" : entity.FileName;
        return result;
    }
}
