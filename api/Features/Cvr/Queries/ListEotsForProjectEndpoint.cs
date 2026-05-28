using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Cvr.Queries;

public sealed class ListEotsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListEotsForProject, IReadOnlyList<Eot>> handler;
    public ListEotsForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListEotsForProject, IReadOnlyList<Eot>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListEotsForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/eots")] HttpRequest request, string projectId)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListEotsForProject(projectId), request.HttpContext.RequestAborted));
    }
}
