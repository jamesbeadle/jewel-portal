
namespace Jewel.JPMS.Api.Features.Manual.Queries;

public sealed class GetManualViewEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly GetManualViewHandler handler;
    public GetManualViewEndpoint(SignedInUserResolver users, GetManualViewHandler handler) { this.users = users; this.handler = handler; }

    [Function(nameof(GetManualView))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manual/views/{view}")] HttpRequest request, string view)
    {
        var cancellationToken = ManualEndpointRequest.CancellationOf(request);
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ManualRoles.AllowedToReadViews.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        if (!Enum.TryParse<ManualView>(view, ignoreCase: true, out var asked)) return new BadRequestObjectResult(new[] { $"There is no view called {view}." });
        return new OkObjectResult(await handler.HandleAsync(new GetManualView(asked), signedInUser.Email, cancellationToken));
    }
}
