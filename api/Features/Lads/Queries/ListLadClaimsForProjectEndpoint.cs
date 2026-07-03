using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Lads.Queries;

public sealed class ListLadClaimsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListLadClaimsForProject, IReadOnlyList<LadClaim>> handler;
    public ListLadClaimsForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListLadClaimsForProject, IReadOnlyList<LadClaim>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListLadClaimsForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/lad-claims")] HttpRequest request, string projectId)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListLadClaimsForProject(projectId), request.HttpContext.RequestAborted));
    }
}
