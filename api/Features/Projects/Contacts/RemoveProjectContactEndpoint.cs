using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Projects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Projects.Contacts;

public sealed class RemoveProjectContactEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ProjectContactAuthorisation authorisation;
    private readonly ICommandHandler<RemoveProjectContact, Acknowledgement> handler;

    public RemoveProjectContactEndpoint(
        SignedInUserResolver users,
        ProjectContactAuthorisation authorisation,
        ICommandHandler<RemoveProjectContact, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.handler = handler;
    }

    [Function(nameof(RemoveProjectContact))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "projects/{projectId}/contacts/{contactId}")] HttpRequest request,
        string projectId,
        string contactId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!authorisation.Allows(signedInUser)) return new StatusCodeResult(403);

        var ack = await handler.HandleAsync(new RemoveProjectContact(projectId, contactId), request.HttpContext.RequestAborted);
        return new OkObjectResult(ack);
    }
}
