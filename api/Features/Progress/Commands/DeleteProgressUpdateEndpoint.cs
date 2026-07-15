using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Progress;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

public sealed class DeleteProgressUpdateEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DeleteProgressUpdateAuthorisation authorisation;
    private readonly ICommandHandler<DeleteProgressUpdate, Acknowledgement> handler;

    public DeleteProgressUpdateEndpoint(
        SignedInUserResolver users,
        DeleteProgressUpdateAuthorisation authorisation,
        ICommandHandler<DeleteProgressUpdate, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.handler = handler;
    }

    [Function(nameof(DeleteProgressUpdate))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "progress-updates/{progressUpdateId}")] HttpRequest request,
        string progressUpdateId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new DeleteProgressUpdate(progressUpdateId);
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var acknowledgement = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(acknowledgement);
    }
}
