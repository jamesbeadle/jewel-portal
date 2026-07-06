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

    [Function(nameof(SearchLocalSubcontractors))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/local-subcontractors")] HttpRequest request,
        string projectId)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();

        var trade = request.Query["trade"].ToString();
        var pageToken = request.Query["pageToken"].ToString();
        var query = new SearchLocalSubcontractors(
            projectId, trade, string.IsNullOrWhiteSpace(pageToken) ? null : pageToken);

        return new OkObjectResult(await handler.HandleAsync(query, request.HttpContext.RequestAborted));
    }
}
