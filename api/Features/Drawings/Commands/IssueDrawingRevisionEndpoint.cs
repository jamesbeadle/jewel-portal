using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

public sealed class IssueDrawingRevisionEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IssueDrawingRevisionAuthorisation authorisation;
    private readonly IssueDrawingRevisionValidation validation;
    private readonly ICommandHandler<IssueDrawingRevision, DrawingRevision> handler;

    public IssueDrawingRevisionEndpoint(
        SignedInUserResolver users,
        IssueDrawingRevisionAuthorisation authorisation,
        IssueDrawingRevisionValidation validation,
        ICommandHandler<IssueDrawingRevision, DrawingRevision> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(IssueDrawingRevision))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "drawings/{drawingId}/revisions")] HttpRequest request,
        string drawingId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<IssueDrawingRevision>();
        if (command is null) return new BadRequestResult();
        if (command.DrawingId != drawingId) return new BadRequestObjectResult("Route drawingId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var revision = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(revision);
    }
}
