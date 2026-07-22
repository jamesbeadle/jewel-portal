using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class SearchLocalSubcontractorsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<SearchLocalSubcontractors, LocalSubcontractorSearchResult> handler;

    public SearchLocalSubcontractorsEndpoint(SignedInUserResolver users, IQueryHandler<SearchLocalSubcontractors, LocalSubcontractorSearchResult> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Procurement reads are internal-only; subcontractor portal sessions get their own
    // scoped endpoints rather than the staff procurement views.
    private static readonly RoleSet RolesThatMayReadProcurement = JpmsRoleSets.AllInternal;

    [Function(nameof(SearchLocalSubcontractors))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/local-subcontractors")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadProcurement.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var trade = request.Query["trade"].ToString();
        var pageToken = request.Query["pageToken"].ToString();
        var query = new SearchLocalSubcontractors(
            projectId, trade, string.IsNullOrWhiteSpace(pageToken) ? null : pageToken);

        return new OkObjectResult(await handler.HandleAsync(query, request.HttpContext.RequestAborted));
    }
}
