using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Leads.Queries;

public sealed class ListInformationChaseItemsForLeadEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListInformationChaseItemsForLead, IReadOnlyList<InfoChaseItem>> handler;

    public ListInformationChaseItemsForLeadEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListInformationChaseItemsForLead, IReadOnlyList<InfoChaseItem>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Lead pipeline reads are internal-only; external portal logins have no business here.
    private static readonly RoleSet RolesThatMayReadLeads = JpmsRoleSets.AllInternal;

    [Function(nameof(ListInformationChaseItemsForLead))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leads/{leadId}/info-chase")] HttpRequest request,
        string leadId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadLeads.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        var items = await handler.HandleAsync(new ListInformationChaseItemsForLead(leadId), request.HttpContext.RequestAborted);
        return new OkObjectResult(items);
    }
}
