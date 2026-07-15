using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Progress.Queries;

public sealed class ListProgressUpdatesForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListProgressUpdatesForProject, IReadOnlyList<ProgressUpdate>> handler;

    public ListProgressUpdatesForProjectEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListProgressUpdatesForProject, IReadOnlyList<ProgressUpdate>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListProgressUpdatesForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/progress-updates")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ProgressRoles.Readers.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        var updates = await handler.HandleAsync(
            new ListProgressUpdatesForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(updates);
    }
}
