using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Cvr.Queries;

public sealed class ListCvrSnapshotsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListCvrSnapshotsForProject, IReadOnlyList<CvrSnapshot>> handler;
    public ListCvrSnapshotsForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListCvrSnapshotsForProject, IReadOnlyList<CvrSnapshot>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListCvrSnapshotsForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/cvr-snapshots")] HttpRequest request, string projectId)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListCvrSnapshotsForProject(projectId), request.HttpContext.RequestAborted));
    }
}
