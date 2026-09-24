using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Api.Features.Parties;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class ListVariationOrdersForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IQueryHandler<ListVariationOrdersForProject, IReadOnlyList<VariationOrder>> handler;

    public ListVariationOrdersForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListVariationOrdersForProject, IReadOnlyList<VariationOrder>> handler, JpmsContext context)
    {
        this.context = context;
        this.users = users;
        this.handler = handler;
    }

    // The delivery team, and the project's client and architect on their own projects with the
    // internal parts stripped (Parties/PartyReads).
    private static readonly RoleSet RolesThatMayReadVariations = JpmsRoleSets.DeliveryTeamAndParties;

    [Function(nameof(ListVariationOrdersForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/variation-orders")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadVariations.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!await PartyReads.MayReadProjectAsync(context, signedInUser, projectId, request.HttpContext.RequestAborted)) return new NotFoundResult();

        var vos = await handler.HandleAsync(new ListVariationOrdersForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(vos.AsReadBy(signedInUser));
    }
}
