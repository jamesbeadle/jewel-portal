using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Cvr.Queries;

public sealed class ListPrelimItemsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListPrelimItemsForProject, IReadOnlyList<PrelimItem>> handler;
    public ListPrelimItemsForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListPrelimItemsForProject, IReadOnlyList<PrelimItem>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListPrelimItemsForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/prelims")] HttpRequest request, string projectId)
    {
        if (users.Resolve(request) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListPrelimItemsForProject(projectId), request.HttpContext.RequestAborted));
    }
}
