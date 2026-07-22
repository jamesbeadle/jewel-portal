using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ListRequestsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListRequestsForProject, IReadOnlyList<Request>> handler;
    public ListRequestsForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListRequestsForProject, IReadOnlyList<Request>> handler) { this.users = users; this.handler = handler; }

    // Request reads are internal plus the architect, who reads/approves RFIs per the permissions matrix.
    private static readonly RoleSet RolesThatMayReadRequests = JpmsRoleSets.InternalAndArchitect;

    [Function(nameof(ListRequestsForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/requests")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadRequests.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListRequestsForProject(projectId), request.HttpContext.RequestAborted));
    }
}
