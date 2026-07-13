using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Cvr.Queries;

public sealed class ListCvrSnapshotsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListCvrSnapshotsForProject, IReadOnlyList<CvrSnapshot>> handler;
    public ListCvrSnapshotsForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListCvrSnapshotsForProject, IReadOnlyList<CvrSnapshot>> handler) { this.users = users; this.handler = handler; }

    // CVR reads are internal-only; external portal logins have no view of margin and forecast.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(ListCvrSnapshotsForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/cvr-snapshots")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListCvrSnapshotsForProject(projectId), request.HttpContext.RequestAborted));
    }
}
