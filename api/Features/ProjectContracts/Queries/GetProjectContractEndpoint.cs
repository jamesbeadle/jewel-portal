using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ProjectContracts;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Queries;

/// <summary>GET /api/projects/{projectId}/contract — the contract terms, or 204 when none recorded.</summary>
public sealed class GetProjectContractEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetProjectContract, ProjectContract?> handler;

    public GetProjectContractEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetProjectContract, ProjectContract?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetProjectContract))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/contract")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ProjectContractRoles.AllowedToReadContract.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var contract = await handler.HandleAsync(new GetProjectContract(projectId), cancellationToken);
        if (contract is null) return new NoContentResult();
        return new OkObjectResult(contract);
    }
}
