using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Leads.Queries;

public sealed class GetBidDecisionForLeadEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetBidDecisionForLead, BidDecision?> handler;

    public GetBidDecisionForLeadEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetBidDecisionForLead, BidDecision?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Lead pipeline reads are internal-only; external portal logins have no business here.
    private static readonly RoleSet RolesThatMayReadLeads = JpmsRoleSets.AllInternal;

    [Function(nameof(GetBidDecisionForLead))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leads/{leadId}/bid-decision")] HttpRequest request,
        string leadId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadLeads.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var bidDecision = await handler.HandleAsync(new GetBidDecisionForLead(leadId), request.HttpContext.RequestAborted);
        return new OkObjectResult(bidDecision);
    }
}
