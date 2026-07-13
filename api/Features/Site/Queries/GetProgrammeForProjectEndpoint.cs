using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Site.Queries;

public sealed class GetProgrammeForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetProgrammeForProject, IReadOnlyList<ProgrammeTask>> handler;
    public GetProgrammeForProjectEndpoint(SignedInUserResolver users, IQueryHandler<GetProgrammeForProject, IReadOnlyList<ProgrammeTask>> handler) { this.users = users; this.handler = handler; }

    // Programme/site reads are internal-only; external portal sessions use their own scoped endpoints.
    private static readonly RoleSet RolesThatMayReadSite = JpmsRoleSets.AllInternal;

    [Function(nameof(GetProgrammeForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/programme")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadSite.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new GetProgrammeForProject(projectId), request.HttpContext.RequestAborted));
    }
}
