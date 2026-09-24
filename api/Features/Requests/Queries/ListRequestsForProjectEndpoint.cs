using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Api.Features.Parties;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ListRequestsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IQueryHandler<ListRequestsForProject, IReadOnlyList<Request>> handler;
    public ListRequestsForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListRequestsForProject, IReadOnlyList<Request>> handler, JpmsContext context) { this.context = context; this.users = users; this.handler = handler; }

    // The delivery team, and the project's client and architect on their own projects with the
    // internal parts stripped (Parties/PartyReads).
    private static readonly RoleSet RolesThatMayReadRequests = JpmsRoleSets.DeliveryTeamAndParties;

    [Function(nameof(ListRequestsForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/requests")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadRequests.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!await PartyReads.MayReadProjectAsync(context, signedInUser, projectId, request.HttpContext.RequestAborted)) return new NotFoundResult();
        var requests = await handler.HandleAsync(new ListRequestsForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(requests.AsReadBy(signedInUser));
    }
}
