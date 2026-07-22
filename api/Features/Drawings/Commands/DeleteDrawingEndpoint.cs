using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Drawings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

/// <summary>
/// DELETE /api/drawings/{drawingId} — permanently removes the drawing, all of its revisions and
/// their stored files. Administrator, Managing Director and Project Manager only.
/// </summary>
public sealed class DeleteDrawingEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DeleteDrawingAuthorisation authorisation;
    private readonly DeleteDrawingValidation validation;
    private readonly ICommandHandler<DeleteDrawing, Acknowledgement> handler;

    public DeleteDrawingEndpoint(
        SignedInUserResolver users,
        DeleteDrawingAuthorisation authorisation,
        DeleteDrawingValidation validation,
        ICommandHandler<DeleteDrawing, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(DeleteDrawing))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "drawings/{drawingId}")] HttpRequest request,
        string drawingId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new DeleteDrawing(drawingId);

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
