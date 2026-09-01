using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Xero.Queries;

public sealed class GetXeroAgedPayablesEndpoint
{
    // The same finance-facing audience as the Xero ledger view — the allocation queue already
    // shows this audience every bill and its amount due; the aged report is an aggregation of
    // the same figures, not a widening. Deliberately looser than the Cash Summary's
    // directors-only gate (no bank balances here). Admins pass because Role.Admin is included
    // explicitly. Accounts joined 2026-08-27: the Weekly Cashflow — the accountant's working
    // tool — is seeded from exactly this read, and the bills owed are his day job anyway.
    private static readonly RoleSet AllowedToViewPayables = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator,
        JpmsRoles.Accounts);

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
        if (!AllowedToViewPayables.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var force = string.Equals(request.Query["force"], "true", StringComparison.OrdinalIgnoreCase);
        var snapshot = await handler.HandleAsync(new GetXeroAgedPayables(force), request.HttpContext.RequestAborted);
        return new OkObjectResult(snapshot);
    }
}
