using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Leads.Queries;

public sealed class GetProposalForLeadEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetProposalForLead, Proposal?> handler;

    public GetProposalForLeadEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetProposalForLead, Proposal?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetProposalForLead))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leads/{leadId}/proposal")] HttpRequest request,
        string leadId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();

        var proposal = await handler.HandleAsync(new GetProposalForLead(leadId), request.HttpContext.RequestAborted);
        return new OkObjectResult(proposal);
    }
}
