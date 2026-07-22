using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Progress.Queries;

public sealed class ListProgressReportsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListProgressReportsForProject, IReadOnlyList<ProgressReport>> handler;

    public ListProgressReportsForProjectEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListProgressReportsForProject, IReadOnlyList<ProgressReport>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListProgressReportsForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/progress-reports")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ProgressRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var reports = await handler.HandleAsync(
            new ListProgressReportsForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(reports);
    }
}
