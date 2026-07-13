using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Closeout.Queries;

public sealed class ListDefectsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListDefectsForProject, IReadOnlyList<Defect>> handler;
    public ListDefectsForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListDefectsForProject, IReadOnlyList<Defect>> handler) { this.users = users; this.handler = handler; }

    // Closeout reads are internal-only; external portal logins have no view here.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(ListDefectsForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/defects")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListDefectsForProject(projectId), request.HttpContext.RequestAborted));
    }
}
