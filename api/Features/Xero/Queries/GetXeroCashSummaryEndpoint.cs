using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Xero.Queries;

public sealed class GetXeroCashSummaryEndpoint
{
    // Bank balances are the company's most sensitive figures — directors only, deliberately
    // tighter than the ledger audience. Admins pass because Role.Admin is included explicitly.
    private static readonly RoleSet AllowedToViewCash = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector);

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
        if (!AllowedToViewCash.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var force = string.Equals(request.Query["force"], "true", StringComparison.OrdinalIgnoreCase);
        var snapshot = await handler.HandleAsync(new GetXeroCashSummary(force), request.HttpContext.RequestAborted);
        return new OkObjectResult(snapshot);
    }
}
