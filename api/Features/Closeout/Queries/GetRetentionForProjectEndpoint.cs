using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Closeout.Queries;

public sealed class GetRetentionForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetRetentionForProject, RetentionRelease?> handler;

    public GetRetentionForProjectEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetRetentionForProject, RetentionRelease?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Closeout reads are internal-only; external portal logins have no view here.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(GetRetentionForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/retention")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        var retention = await handler.HandleAsync(new GetRetentionForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(retention);
    }
}
