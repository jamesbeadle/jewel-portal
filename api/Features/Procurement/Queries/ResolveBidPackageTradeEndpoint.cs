using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class ResolveBidPackageTradeEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ResolveBidPackageTrade, BidPackageTradeResolution> handler;

    public ResolveBidPackageTradeEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ResolveBidPackageTrade, BidPackageTradeResolution> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Same gate as the other procurement reads (SearchLocalSubcontractors above all — this call
    // exists to feed that one its search term).
    private static readonly RoleSet RolesThatMayReadProcurement = JpmsRoleSets.AllInternal;

    [Function(nameof(ResolveBidPackageTrade))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bid-packages/{bidPackageId}/trade-resolution")] HttpRequest request,
        string bidPackageId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadProcurement.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        return new OkObjectResult(await handler.HandleAsync(
            new ResolveBidPackageTrade(bidPackageId), request.HttpContext.RequestAborted));
    }
}
