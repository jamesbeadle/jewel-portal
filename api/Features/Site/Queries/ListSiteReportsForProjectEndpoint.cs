using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Site.Queries;

public sealed class ListSiteReportsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListSiteReportsForProject, IReadOnlyList<SiteReport>> handler;
    public ListSiteReportsForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListSiteReportsForProject, IReadOnlyList<SiteReport>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListSiteReportsForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/site-reports")] HttpRequest request, string projectId)
    {
        if (users.Resolve(request) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListSiteReportsForProject(projectId), request.HttpContext.RequestAborted));
    }
}
