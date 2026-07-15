using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Retention.Queries;

public sealed class GetProjectRetentionEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetProjectRetention, ProjectRetention?> handler;

    public GetProjectRetentionEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetProjectRetention, ProjectRetention?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Retention terms are internal commercial data; external portal logins have no view here.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(GetProjectRetention))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/retention-terms")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        var retention = await handler.HandleAsync(new GetProjectRetention(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(retention);
    }
}
