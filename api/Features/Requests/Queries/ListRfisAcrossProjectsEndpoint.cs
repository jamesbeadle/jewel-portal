using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ListRfisAcrossProjectsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListRfisAcrossProjects, IReadOnlyList<Request>> handler;
    public ListRfisAcrossProjectsEndpoint(SignedInUserResolver users, IQueryHandler<ListRfisAcrossProjects, IReadOnlyList<Request>> handler) { this.users = users; this.handler = handler; }

    // Route lives under /rfis (not /requests/rfis) so it can never be shadowed by the
    // GetRequestById route template "requests/{requestId}".
    [Function(nameof(ListRfisAcrossProjects))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "rfis")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RfiDashboardRoles.AllowedToViewDashboard.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListRfisAcrossProjects(), request.HttpContext.RequestAborted));
    }
}
