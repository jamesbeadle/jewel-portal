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

    [Function(nameof(ListRfisAcrossProjects))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/rfis")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RfiDashboardRoles.AllowedToViewDashboard.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListRfisAcrossProjects(), request.HttpContext.RequestAborted));
    }
}
