using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Queries;

public sealed class ListDrawingFoldersForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IQueryHandler<ListDrawingFoldersForProject, IReadOnlyList<DrawingFolder>> handler;

    public ListDrawingFoldersForProjectEndpoint(
        SignedInUserResolver users,
        JpmsContext context,
        IQueryHandler<ListDrawingFoldersForProject, IReadOnlyList<DrawingFolder>> handler)
    {
        this.users = users;
        this.context = context;
        this.handler = handler;
    }

    // Folders are part of reading the register, so the read gate matches ListDrawingsForProject.
    private static readonly RoleSet RolesThatMayReadDrawings = JpmsRoleSets.DrawingReaders;

    [Function(nameof(ListDrawingFoldersForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/drawing-folders")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadDrawings.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!await DrawingScope.MayReadProjectAsync(context, signedInUser, projectId, cancellationToken))
            return new StatusCodeResult(403);

        var folders = await handler.HandleAsync(
            new ListDrawingFoldersForProject(projectId), cancellationToken);
        return new OkObjectResult(folders);
    }
}
