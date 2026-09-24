using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ListRequestsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListRequestsForProject, IReadOnlyList<Request>> handler;
    public ListRequestsForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListRequestsForProject, IReadOnlyList<Request>> handler) { this.users = users; this.handler = handler; }

    // Internal only: a request carries the business's notes and mail (2026-09-24).
    private static readonly RoleSet RolesThatMayReadRequests = JpmsRoleSets.ProjectDeliveryTeam;

    [Function(nameof(ListRequestsForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/requests")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadRequests.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListRequestsForProject(projectId), request.HttpContext.RequestAborted));
    }
}
