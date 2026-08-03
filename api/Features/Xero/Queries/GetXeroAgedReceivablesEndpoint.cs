using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Xero.Queries;

public sealed class GetXeroAgedReceivablesEndpoint
{
    // The same finance-facing audience as Aged Payables — receivables are the valuation
    // invoices this audience already raises and tracks, aggregated by client, not a widening.
    // Deliberately looser than the Cash Summary's directors-only gate (no bank balances here).
    // Admins pass because Role.Admin is included explicitly.
    private static readonly RoleSet AllowedToViewReceivables = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

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
        if (!AllowedToViewReceivables.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var force = string.Equals(request.Query["force"], "true", StringComparison.OrdinalIgnoreCase);
        var snapshot = await handler.HandleAsync(new GetXeroAgedReceivables(force), request.HttpContext.RequestAborted);
        return new OkObjectResult(snapshot);
    }
}
