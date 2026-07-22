using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ListUnassignedRequestsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListUnassignedRequests, IReadOnlyList<Request>> handler;
    public ListUnassignedRequestsEndpoint(SignedInUserResolver users, IQueryHandler<ListUnassignedRequests, IReadOnlyList<Request>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListUnassignedRequests))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/unassigned")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TriageRoles.AllowedToTriage.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListUnassignedRequests(), request.HttpContext.RequestAborted));
    }
}
