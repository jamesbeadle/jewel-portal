using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

/// <summary>
/// POST /api/drawings/data-rows/rebuild — body { projectId?, force? }. Queues the rows-only
/// re-transcription for revisions extracted before the row tables existed. Same plumbing as
/// every command endpoint; the connector's rebuild_document_data action runs the same handler.
/// </summary>
public sealed class RebuildDrawingDataRowsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly RebuildDrawingDataRowsAuthorisation authorisation;
    private readonly RebuildDrawingDataRowsValidation validation;
    private readonly ICommandHandler<RebuildDrawingDataRows, DrawingDataRowsRebuild> handler;

    public RebuildDrawingDataRowsEndpoint(
        SignedInUserResolver users, AuditActor auditActor,
        RebuildDrawingDataRowsAuthorisation authorisation,
        RebuildDrawingDataRowsValidation validation,
        ICommandHandler<RebuildDrawingDataRows, DrawingDataRowsRebuild> handler)
    {
        this.users = users; this.auditActor = auditActor;
        this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function("RebuildDrawingDataRows")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "drawings/data-rows/rebuild")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        auditActor.Email = signedInUser.Email;

        var command = await request.ReadFromJsonAsync<RebuildDrawingDataRows>() ?? new RebuildDrawingDataRows(null);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try { return new OkObjectResult(await handler.HandleAsync(command, cancellationToken)); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }
    }
}
