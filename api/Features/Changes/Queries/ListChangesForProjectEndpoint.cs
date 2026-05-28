using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Changes;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Changes.Queries;

public sealed class ListChangesForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListChangesForProject, IReadOnlyList<ChangeRecord>> handler;
    public ListChangesForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListChangesForProject, IReadOnlyList<ChangeRecord>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListChangesForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/changes")] HttpRequest request, string projectId)
    {
        if (users.Resolve(request) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListChangesForProject(projectId), request.HttpContext.RequestAborted));
    }
}
