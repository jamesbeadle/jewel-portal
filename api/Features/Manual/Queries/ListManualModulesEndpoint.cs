
namespace Jewel.JPMS.Api.Features.Manual.Queries;

public sealed class ListManualModulesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListManualModules, IReadOnlyList<ManualModule>> handler;
    public ListManualModulesEndpoint(SignedInUserResolver users, IQueryHandler<ListManualModules, IReadOnlyList<ManualModule>> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(ListManualModules))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manual/modules")] HttpRequest request)
    {
        var cancellationToken = ManualEndpointRequest.CancellationOf(request);
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ManualRoles.AllowedToReadMaster.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return new OkObjectResult(await handler.HandleAsync(new ListManualModules(), cancellationToken));
    }
}
