using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Queries;

public sealed class GetXeroAgedReceivablesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetXeroAgedReceivables, XeroAgedReceivablesSnapshot> handler;

    public GetXeroAgedReceivablesEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetXeroAgedReceivables, XeroAgedReceivablesSnapshot> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetXeroAgedReceivables))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "xero/aged-receivables")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!XeroReportRoles.AgedReportReaders.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var force = string.Equals(request.Query["force"], "true", StringComparison.OrdinalIgnoreCase);
        var snapshot = await handler.HandleAsync(new GetXeroAgedReceivables(force), request.HttpContext.RequestAborted);
        return new OkObjectResult(snapshot);
    }
}
