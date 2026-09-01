using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Drawings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

/// <summary>
/// The extraction routes: queue one revision, queue a project's backlog, read a revision's data
/// view. Same plumbing as every command endpoint — resolve the user, gate, run, surface
/// InvalidOperationException as a readable 400.
/// </summary>
public sealed class DrawingExtractionEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly QueueDrawingExtractionAuthorisation queueAuthorisation;
    private readonly QueueDrawingExtractionValidation queueValidation;
    private readonly ICommandHandler<QueueDrawingExtraction, DrawingExtraction> queueHandler;
    private readonly QueueProjectDrawingExtractionsAuthorisation bulkAuthorisation;
    private readonly QueueProjectDrawingExtractionsValidation bulkValidation;
    private readonly ICommandHandler<QueueProjectDrawingExtractions, int> bulkHandler;
    private readonly IQueryHandler<GetDrawingExtraction, DrawingExtractionView?> viewHandler;

    public DrawingExtractionEndpoints(
        SignedInUserResolver users, AuditActor auditActor,
        QueueDrawingExtractionAuthorisation queueAuthorisation,
        QueueDrawingExtractionValidation queueValidation,
        ICommandHandler<QueueDrawingExtraction, DrawingExtraction> queueHandler,
        QueueProjectDrawingExtractionsAuthorisation bulkAuthorisation,
        QueueProjectDrawingExtractionsValidation bulkValidation,
        ICommandHandler<QueueProjectDrawingExtractions, int> bulkHandler,
        IQueryHandler<GetDrawingExtraction, DrawingExtractionView?> viewHandler)
    {
        this.users = users; this.auditActor = auditActor;
        this.queueAuthorisation = queueAuthorisation; this.queueValidation = queueValidation;
        this.queueHandler = queueHandler;
        this.bulkAuthorisation = bulkAuthorisation; this.bulkValidation = bulkValidation;
        this.bulkHandler = bulkHandler;
        this.viewHandler = viewHandler;
    }

    [Function("QueueDrawingExtraction")]
    public async Task<IActionResult> QueueOne(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "drawings/{drawingId}/revisions/{revisionId}/extract")] HttpRequest request,
        string drawingId, string revisionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        auditActor.Email = signedInUser.Email;

        var command = await request.ReadFromJsonAsync<QueueDrawingExtraction>();
        if (command is null) return new BadRequestResult();
        if (command.DrawingId != drawingId || command.DrawingRevisionId != revisionId)
            return new BadRequestObjectResult("Route ids do not match body.");

        if (!queueAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = queueValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try { return new OkObjectResult(await queueHandler.HandleAsync(command, cancellationToken)); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }
    }

    [Function("QueueProjectDrawingExtractions")]
    public async Task<IActionResult> QueueBacklog(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/drawings/extract-all")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        auditActor.Email = signedInUser.Email;

        var command = new QueueProjectDrawingExtractions(projectId);
        if (!bulkAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = bulkValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try { return new OkObjectResult(await bulkHandler.HandleAsync(command, cancellationToken)); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }
    }

    // Keyed by revision alone, like the file download route — the drawing id adds nothing.
    [Function("GetDrawingExtraction")]
    public async Task<IActionResult> View(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "drawings/revisions/{revisionId}/extraction")] HttpRequest request,
        string revisionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var view = await viewHandler.HandleAsync(new GetDrawingExtraction(revisionId), cancellationToken);
        return new OkObjectResult(view);
    }
}
