using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListCostCentreGroupsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListCostCentreGroupsForProject, IReadOnlyList<CostCentreGroup>> handler;

    public ListCostCentreGroupsForProjectEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListCostCentreGroupsForProject, IReadOnlyList<CostCentreGroup>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListCostCentreGroupsForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/cost-centre-groups")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var groups = await handler.HandleAsync(new ListCostCentreGroupsForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(groups);
    }
}
