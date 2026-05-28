using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListTimesheetsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListTimesheetsForProject, IReadOnlyList<Timesheet>> handler;
    public ListTimesheetsForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListTimesheetsForProject, IReadOnlyList<Timesheet>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListTimesheetsForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/timesheets")] HttpRequest request, string projectId)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListTimesheetsForProject(projectId), request.HttpContext.RequestAborted));
    }
}
