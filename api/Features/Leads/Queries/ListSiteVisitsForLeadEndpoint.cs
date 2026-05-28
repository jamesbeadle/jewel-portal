using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Leads.Queries;

public sealed class ListSiteVisitsForLeadEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListSiteVisitsForLead, IReadOnlyList<SiteVisit>> handler;

    public ListSiteVisitsForLeadEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListSiteVisitsForLead, IReadOnlyList<SiteVisit>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListSiteVisitsForLead))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leads/{leadId}/site-visits")] HttpRequest request,
        string leadId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var visits = await handler.HandleAsync(new ListSiteVisitsForLead(leadId), request.HttpContext.RequestAborted);
        return new OkObjectResult(visits);
    }
}
