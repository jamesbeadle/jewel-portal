using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class ListVariationOrdersForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListVariationOrdersForProject, IReadOnlyList<VariationOrder>> handler;

    public ListVariationOrdersForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListVariationOrdersForProject, IReadOnlyList<VariationOrder>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Variation reads are internal plus the architect, who reads/approves variations per the permissions matrix.
    private static readonly RoleSet RolesThatMayReadVariations = JpmsRoleSets.InternalAndArchitect;

    [Function(nameof(ListVariationOrdersForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/variation-orders")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadVariations.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var vos = await handler.HandleAsync(new ListVariationOrdersForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(vos);
    }
}
