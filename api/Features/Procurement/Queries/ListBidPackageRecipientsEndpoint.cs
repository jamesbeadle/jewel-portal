using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class ListBidPackageRecipientsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListBidPackageRecipients, IReadOnlyList<BidPackageRecipient>> handler;

    public ListBidPackageRecipientsEndpoint(SignedInUserResolver users, IQueryHandler<ListBidPackageRecipients, IReadOnlyList<BidPackageRecipient>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Procurement reads are internal-only; subcontractor portal sessions get their own
    // scoped endpoints rather than the staff procurement views.
    private static readonly RoleSet RolesThatMayReadProcurement = JpmsRoleSets.AllInternal;

    [Function(nameof(ListBidPackageRecipients))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bid-packages/{bidPackageId}/recipients")] HttpRequest request,
        string bidPackageId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadProcurement.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListBidPackageRecipients(bidPackageId), request.HttpContext.RequestAborted));
    }
}
