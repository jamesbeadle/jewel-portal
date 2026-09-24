using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Api.Features.Parties;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class GetVariationOrderByIdEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IQueryHandler<GetVariationOrderById, VariationOrder?> handler;

    public GetVariationOrderByIdEndpoint(SignedInUserResolver users, IQueryHandler<GetVariationOrderById, VariationOrder?> handler, JpmsContext context)
    {
        this.context = context;
        this.users = users;
        this.handler = handler;
    }

    // The delivery team, and the project's client and architect on their own projects with the
    // internal parts stripped (Parties/PartyReads).
    private static readonly RoleSet RolesThatMayReadVariations = JpmsRoleSets.DeliveryTeamAndParties;

    [Function(nameof(GetVariationOrderById))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "variation-orders/{voId}")] HttpRequest request,
        string voId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadVariations.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!await PartyReads.MayReadVariationAsync(context, signedInUser, voId, request.HttpContext.RequestAborted)) return new NotFoundResult();

        var vo = await handler.HandleAsync(new GetVariationOrderById(voId), request.HttpContext.RequestAborted);
        return new OkObjectResult(vo?.AsReadBy(signedInUser));
    }
}
