using Jewel.JPMS.Contracts.AccessRequests;

namespace Jewel.JPMS.Api.Features.AccessRequests.Queries;

public sealed class ListPendingAccessRequestsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListPendingAccessRequests, IReadOnlyList<AccessRequest>> handler;

    public ListPendingAccessRequestsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListPendingAccessRequests, IReadOnlyList<AccessRequest>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListPendingAccessRequests))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "access-requests")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AdminGate.Allows(signedInUser)) return new StatusCodeResult(403);

        var requests = await handler.HandleAsync(new ListPendingAccessRequests(), request.HttpContext.RequestAborted);
        return new OkObjectResult(requests);
    }
}
