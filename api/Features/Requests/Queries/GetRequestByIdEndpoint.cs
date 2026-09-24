using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class GetRequestByIdEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetRequestById, Request?> handler;

    public GetRequestByIdEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetRequestById, Request?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Internal only: a request carries the business's notes and mail (2026-09-24).
    private static readonly RoleSet RolesThatMayReadRequests = JpmsRoleSets.ProjectDeliveryTeam;

    [Function(nameof(GetRequestById))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/{requestId}")] HttpRequest request,
        string requestId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadRequests.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var change = await handler.HandleAsync(new GetRequestById(requestId), request.HttpContext.RequestAborted);
        return new OkObjectResult(change);
    }
}
