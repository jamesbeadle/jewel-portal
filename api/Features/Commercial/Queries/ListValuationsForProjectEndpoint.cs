using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListValuationsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListValuationsForProject, IReadOnlyList<Valuation>> handler;
    public ListValuationsForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListValuationsForProject, IReadOnlyList<Valuation>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListValuationsForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/valuations")] HttpRequest request, string projectId)
    {
        if (users.Resolve(request) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListValuationsForProject(projectId), request.HttpContext.RequestAborted));
    }
}
