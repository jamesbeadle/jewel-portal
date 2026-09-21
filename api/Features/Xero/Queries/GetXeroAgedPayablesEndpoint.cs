using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Queries;

public sealed class GetXeroAgedPayablesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetXeroAgedPayables, XeroAgedPayablesSnapshot> handler;

    public GetXeroAgedPayablesEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetXeroAgedPayables, XeroAgedPayablesSnapshot> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetXeroAgedPayables))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "xero/aged-payables")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!XeroReportRoles.AgedReportReaders.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var force = string.Equals(request.Query["force"], "true", StringComparison.OrdinalIgnoreCase);
        var snapshot = await handler.HandleAsync(new GetXeroAgedPayables(force), request.HttpContext.RequestAborted);
        return new OkObjectResult(snapshot);
    }
}
