using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Drawings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

/// <summary>
/// DELETE /api/drawings/{drawingId}/revisions/{revisionId} — permanently removes one revision and
/// its stored file. Administrator, Managing Director and Project Manager only.
/// </summary>
public sealed class DeleteDrawingRevisionEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DeleteDrawingRevisionAuthorisation authorisation;
    private readonly DeleteDrawingRevisionValidation validation;
    private readonly ICommandHandler<DeleteDrawingRevision, Acknowledgement> handler;

    public DeleteDrawingRevisionEndpoint(
        SignedInUserResolver users,
        DeleteDrawingRevisionAuthorisation authorisation,
        DeleteDrawingRevisionValidation validation,
        ICommandHandler<DeleteDrawingRevision, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(DeleteDrawingRevision))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "drawings/{drawingId}/revisions/{revisionId}")] HttpRequest request,
        string drawingId,
        string revisionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new DeleteDrawingRevision(drawingId, revisionId);

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
