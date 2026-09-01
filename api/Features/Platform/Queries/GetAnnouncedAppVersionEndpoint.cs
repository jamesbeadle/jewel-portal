using Jewel.JPMS.Contracts.Platform;

namespace Jewel.JPMS.Api.Features.Platform.Queries;

/// <summary>
/// GET /api/system/version — the announced version with its publish audit, for Admin → System.
/// Behind the admin gate with the publish command it sits next to: the bare number is public
/// (/api/version), but who published it and when is administration.
/// </summary>
public sealed class GetAnnouncedAppVersionEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetAnnouncedAppVersion, AnnouncedAppVersion> handler;

    public GetAnnouncedAppVersionEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetAnnouncedAppVersion, AnnouncedAppVersion> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetAnnouncedAppVersion))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "system/version")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AdminGate.Allows(signedInUser)) return new StatusCodeResult(403);

        var announced = await handler.HandleAsync(new GetAnnouncedAppVersion(), request.HttpContext.RequestAborted);
        return new OkObjectResult(announced);
    }
}
