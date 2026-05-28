using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Closeout.Queries;

public sealed class GetSettlementForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetSettlementForProject, SettlementRecord?> handler;
    public GetSettlementForProjectEndpoint(SignedInUserResolver users, IQueryHandler<GetSettlementForProject, SettlementRecord?> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(GetSettlementForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/settlement")] HttpRequest request, string projectId)
    {
        if (users.Resolve(request) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new GetSettlementForProject(projectId), request.HttpContext.RequestAborted));
    }
}
