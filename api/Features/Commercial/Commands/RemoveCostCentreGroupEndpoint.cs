using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class RemoveCostCentreGroupEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateCostCentreGroupAuthorisation authorisation; // shared: create/remove carry the same roles
    private readonly ICommandHandler<RemoveCostCentreGroup, Acknowledgement> handler;

    public RemoveCostCentreGroupEndpoint(
        SignedInUserResolver users,
        CreateCostCentreGroupAuthorisation authorisation,
        ICommandHandler<RemoveCostCentreGroup, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.handler = handler;
    }

    [Function(nameof(RemoveCostCentreGroup))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "projects/{projectId}/cost-centre-groups/{groupId}")] HttpRequest request,
        string projectId,
        string groupId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new RemoveCostCentreGroup(projectId, groupId);
        // Readable 403 rather than ForbidResult — see CreateCostCentreGroupEndpoint.
        if (!authorisation.Allows(signedInUser, command))
            return new ObjectResult("Your role doesn't have permission to manage cost centre groups.")
            { StatusCode = StatusCodes.Status403Forbidden };

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
