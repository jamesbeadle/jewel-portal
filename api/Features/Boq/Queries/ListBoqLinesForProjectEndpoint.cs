using Jewel.JPMS.Contracts.Boq;

namespace Jewel.JPMS.Api.Features.Boq.Queries;

public sealed class ListBoqLinesForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListBoqLinesForProject, IReadOnlyList<BoqLineItem>> handler;

    public ListBoqLinesForProjectEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListBoqLinesForProject, IReadOnlyList<BoqLineItem>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // BoQ reads are internal-only; external portal logins have no view of the priced bill.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(ListBoqLinesForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/boq")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var lines = await handler.HandleAsync(new ListBoqLinesForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(lines);
    }
}
