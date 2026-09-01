using Jewel.JPMS.Contracts.Leads;

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

    // Lead pipeline reads are internal-only; external portal logins have no business here.
    private static readonly RoleSet RolesThatMayReadLeads = JpmsRoleSets.AllInternal;

    [Function(nameof(ListSiteVisitsForLead))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leads/{leadId}/site-visits")] HttpRequest request,
        string leadId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadLeads.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var visits = await handler.HandleAsync(new ListSiteVisitsForLead(leadId), request.HttpContext.RequestAborted);
        return new OkObjectResult(visits);
    }
}
