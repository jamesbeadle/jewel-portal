using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Cvr.Queries;

public sealed class ListForecastComponentsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListForecastComponentsForProject, IReadOnlyList<ForecastComponent>> handler;
    public ListForecastComponentsForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListForecastComponentsForProject, IReadOnlyList<ForecastComponent>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListForecastComponentsForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/forecast-components")] HttpRequest request, string projectId)
    {
        if (users.Resolve(request) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListForecastComponentsForProject(projectId), request.HttpContext.RequestAborted));
    }
}
