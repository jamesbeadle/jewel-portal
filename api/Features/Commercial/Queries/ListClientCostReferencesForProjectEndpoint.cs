using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListClientCostReferencesForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListClientCostReferencesForProject, IReadOnlyList<ClientCostReference>> handler;
    public ListClientCostReferencesForProjectEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListClientCostReferencesForProject, IReadOnlyList<ClientCostReference>> handler)
    { this.users = users; this.handler = handler; }

    // The map is part of the valuation report's setup — internal-only, like every other
    // commercial read; the client sees the result on the PDF, never the map itself.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(ListClientCostReferencesForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/client-cost-references")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var query = new ListClientCostReferencesForProject(projectId);
        return new OkObjectResult(await handler.HandleAsync(query, request.HttpContext.RequestAborted));
    }
}
