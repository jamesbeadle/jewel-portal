using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

/// <summary>
/// POST /api/drawings/{drawingId}/revisions/{revisionId}/approve — approves the revision. The
/// approver is the signed-in user. No request body is required.
/// </summary>
public sealed class ApproveDrawingRevisionEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ApproveDrawingRevisionAuthorisation authorisation;
    private readonly ApproveDrawingRevisionValidation validation;
    private readonly ICommandHandler<ApproveDrawingRevision, DrawingRevision> handler;

    public ApproveDrawingRevisionEndpoint(
        SignedInUserResolver users,
        ApproveDrawingRevisionAuthorisation authorisation,
        ApproveDrawingRevisionValidation validation,
        ICommandHandler<ApproveDrawingRevision, DrawingRevision> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(ApproveDrawingRevision))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "drawings/{drawingId}/revisions/{revisionId}/approve")] HttpRequest request,
        string drawingId,
        string revisionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new ApproveDrawingRevision(drawingId, revisionId, signedInUser.Email);

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var revision = await handler.HandleAsync(command, cancellationToken);
        return new OkObjectResult(revision);
    }
}
