using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Queries;

public sealed class GetXeroCashSummaryEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetXeroCashSummary, XeroCashSummarySnapshot> handler;

    public GetXeroCashSummaryEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetXeroCashSummary, XeroCashSummarySnapshot> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetXeroCashSummary))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "xero/cash-summary")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!BankPositionGates.Admits(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var force = string.Equals(request.Query["force"], "true", StringComparison.OrdinalIgnoreCase);
        var snapshot = await handler.HandleAsync(new GetXeroCashSummary(force), request.HttpContext.RequestAborted);
        return new OkObjectResult(snapshot);
    }
}
