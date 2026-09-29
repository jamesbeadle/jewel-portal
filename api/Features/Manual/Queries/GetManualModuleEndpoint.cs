
namespace Jewel.JPMS.Api.Features.Manual.Queries;

public sealed class ManualModuleDetailEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetManualModule, ManualModuleDetail?> handler;
    public ManualModuleDetailEndpoint(SignedInUserResolver users, IQueryHandler<GetManualModule, ManualModuleDetail?> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(GetManualModule))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manual/modules/{manualModuleId}")] HttpRequest request, string manualModuleId)
    {
        var cancellationToken = ManualEndpointRequest.CancellationOf(request);
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ManualRoles.AllowedToReadMaster.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        var detail = await handler.HandleAsync(new GetManualModule(manualModuleId), cancellationToken);
        if (detail is null) return new NotFoundResult();
        return new OkObjectResult(detail);
    }
}
