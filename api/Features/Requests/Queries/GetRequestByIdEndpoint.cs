using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Api.Features.Parties;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class GetRequestByIdEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IQueryHandler<GetRequestById, Request?> handler;

    public GetRequestByIdEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetRequestById, Request?> handler,
        JpmsContext context)
    {
        this.context = context;
        this.users = users;
        this.handler = handler;
    }

    // The delivery team, and the project's client and architect on their own projects with the
    // internal parts stripped (Parties/PartyReads).
    private static readonly RoleSet RolesThatMayReadRequests = JpmsRoleSets.DeliveryTeamAndParties;

    [Function(nameof(GetRequestById))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/{requestId}")] HttpRequest request,
        string requestId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadRequests.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!await PartyReads.MayReadRequestAsync(context, signedInUser, requestId, request.HttpContext.RequestAborted)) return new NotFoundResult();

        var change = await handler.HandleAsync(new GetRequestById(requestId), request.HttpContext.RequestAborted);
        return new OkObjectResult(change?.AsReadBy(signedInUser));
    }
}
