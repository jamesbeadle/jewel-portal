using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Xero.SitePnl;

/// <summary>
/// Who may read the site P&amp;L and trigger its sync: the same finance-facing audience as
/// the Xero ledger — these are the company's job-by-job margins. Admins pass because
/// Role.Admin is included explicitly.
/// </summary>
internal static class XeroSitePnlRoles
{
    public static readonly RoleSet AllowedToView = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);
}

public sealed class GetXeroSitePnlEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetXeroSitePnl, XeroSitePnlSnapshot> handler;

    public GetXeroSitePnlEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetXeroSitePnl, XeroSitePnlSnapshot> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetXeroSitePnl))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "xero/site-pnl")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!XeroSitePnlRoles.AllowedToView.IncludesAny(signedInUser.Roles))
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var snapshot = await handler.HandleAsync(new GetXeroSitePnl(), request.HttpContext.RequestAborted);
        return new OkObjectResult(snapshot);
    }
}

public sealed class SyncXeroSitePnlEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<SyncXeroSitePnl, XeroSitePnlSyncResult> handler;

    public SyncXeroSitePnlEndpoint(
        SignedInUserResolver users,
        ICommandHandler<SyncXeroSitePnl, XeroSitePnlSyncResult> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(SyncXeroSitePnl))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "xero/site-pnl/sync")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!XeroSitePnlRoles.AllowedToView.IncludesAny(signedInUser.Roles))
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var result = await handler.HandleAsync(new SyncXeroSitePnl(), request.HttpContext.RequestAborted);
        return new OkObjectResult(result);
    }
}
