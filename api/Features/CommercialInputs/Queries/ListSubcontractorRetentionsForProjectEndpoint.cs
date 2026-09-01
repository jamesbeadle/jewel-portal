using Jewel.JPMS.Contracts.CommercialInputs;

namespace Jewel.JPMS.Api.Features.CommercialInputs.Queries;

public sealed class ListSubcontractorRetentionsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListSubcontractorRetentionsForProject, IReadOnlyList<SubcontractorRetention>> handler;

    public ListSubcontractorRetentionsForProjectEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListSubcontractorRetentionsForProject, IReadOnlyList<SubcontractorRetention>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Commercial input reads are internal-only; a subcontractor portal login must not see the
    // whole project's retentions.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(ListSubcontractorRetentionsForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/subcontractor-retentions")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var retentions = await handler.HandleAsync(new ListSubcontractorRetentionsForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(retentions);
    }
}
