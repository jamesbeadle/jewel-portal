using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

/// <summary>PUT /api/drawings/{drawingId}/revisions/{revisionId}/label — sets or clears a revision's label.</summary>
public sealed class SetDrawingRevisionLabelEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetDrawingRevisionLabelAuthorisation authorisation;
    private readonly SetDrawingRevisionLabelValidation validation;
    private readonly ICommandHandler<SetDrawingRevisionLabel, DrawingRevision> handler;

    public SetDrawingRevisionLabelEndpoint(
        SignedInUserResolver users,
        SetDrawingRevisionLabelAuthorisation authorisation,
        SetDrawingRevisionLabelValidation validation,
        ICommandHandler<SetDrawingRevisionLabel, DrawingRevision> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SetDrawingRevisionLabel))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "drawings/{drawingId}/revisions/{revisionId}/label")] HttpRequest request,
        string drawingId, string revisionId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<SetDrawingRevisionLabel>();
        if (command is null) return new BadRequestResult();
        if (command.DrawingId != drawingId) return new BadRequestObjectResult("Route drawingId does not match body.");
        if (command.DrawingRevisionId != revisionId) return new BadRequestObjectResult("Route revisionId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            var revision = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
            return new OkObjectResult(revision);
        }
        catch (InvalidOperationException ex)
        {
            // Not-found guards surface as a 400 with the message, never a bodiless 500.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
