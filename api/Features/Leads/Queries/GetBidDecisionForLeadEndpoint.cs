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

    [Function(nameof(GetBidDecisionForLead))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leads/{leadId}/bid-decision")] HttpRequest request,
        string leadId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var bidDecision = await handler.HandleAsync(new GetBidDecisionForLead(leadId), request.HttpContext.RequestAborted);
        return new OkObjectResult(bidDecision);
    }
}
