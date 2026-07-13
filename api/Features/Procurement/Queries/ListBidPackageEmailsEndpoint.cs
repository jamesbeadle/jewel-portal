using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class ListBidPackageEmailsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListBidPackageEmails, IReadOnlyList<MailboxMessage>> handler;

    public ListBidPackageEmailsEndpoint(SignedInUserResolver users, IQueryHandler<ListBidPackageEmails, IReadOnlyList<MailboxMessage>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Procurement reads are internal-only; subcontractor portal sessions get their own
    // scoped endpoints rather than the staff procurement views.
    private static readonly RoleSet RolesThatMayReadProcurement = JpmsRoleSets.AllInternal;

    [Function(nameof(ListBidPackageEmails))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bid-packages/{bidPackageId}/emails")] HttpRequest request,
        string bidPackageId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadProcurement.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListBidPackageEmails(bidPackageId), request.HttpContext.RequestAborted));
    }
}
