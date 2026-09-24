using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Api.Features.Parties;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class GetVoqByRequestEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IQueryHandler<GetVoqByRequest, VariationOrder?> handler;

    public GetVoqByRequestEndpoint(SignedInUserResolver users, IQueryHandler<GetVoqByRequest, VariationOrder?> handler, JpmsContext context)
    {
        this.context = context;
        this.users = users;
        this.handler = handler;
    }

    // The delivery team, and the project's client and architect on their own projects with the
    // internal parts stripped (Parties/PartyReads).
    private static readonly RoleSet RolesThatMayReadVariations = JpmsRoleSets.DeliveryTeamAndParties;

    [Function(nameof(GetVoqByRequest))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/{requestId}/voq")] HttpRequest request,
        string requestId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadVariations.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!await PartyReads.MayReadRequestAsync(context, signedInUser, requestId, request.HttpContext.RequestAborted)) return new NotFoundResult();

        var voq = await handler.HandleAsync(new GetVoqByRequest(requestId), request.HttpContext.RequestAborted);
        var isVisible = voq is not null && await PartyReads.MayReadVariationAsync(context, signedInUser, voq.VariationOrderId, request.HttpContext.RequestAborted);
        return new OkObjectResult(isVisible ? voq!.AsReadBy(signedInUser) : null);
    }
}
