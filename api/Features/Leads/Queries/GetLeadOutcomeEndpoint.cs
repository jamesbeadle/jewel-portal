using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Leads.Queries;

public sealed class GetLeadOutcomeEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetLeadOutcome, LeadOutcome?> handler;

    public GetLeadOutcomeEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetLeadOutcome, LeadOutcome?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetLeadOutcome))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leads/{leadId}/outcome")] HttpRequest request,
        string leadId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();

        var outcome = await handler.HandleAsync(new GetLeadOutcome(leadId), request.HttpContext.RequestAborted);
        return new OkObjectResult(outcome);
    }
}
