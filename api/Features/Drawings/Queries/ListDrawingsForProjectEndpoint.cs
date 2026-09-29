using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Queries;

public sealed class ListDrawingsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IQueryHandler<ListDrawingsForProject, IReadOnlyList<Drawing>> handler;

    public ListDrawingsForProjectEndpoint(
        SignedInUserResolver users,
        JpmsContext context,
        IQueryHandler<ListDrawingsForProject, IReadOnlyList<Drawing>> handler)
    {
        this.users = users;
        this.context = context;
        this.handler = handler;
    }

    // Drawing reads span internal roles plus the externals who work from drawings (architect, subcontractor).
    private static readonly RoleSet RolesThatMayReadDrawings = JpmsRoleSets.DrawingReaders;

    [Function(nameof(ListDrawingsForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/drawings")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadDrawings.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!await DrawingScope.MayReadProjectAsync(context, signedInUser, projectId, cancellationToken))
            return new StatusCodeResult(403);

        var approvedOnly = string.Equals(request.Query["approvedOnly"], "true", StringComparison.OrdinalIgnoreCase);

        var drawings = await handler.HandleAsync(
            new ListDrawingsForProject(projectId, approvedOnly), cancellationToken);
        return new OkObjectResult(drawings);
    }
}
