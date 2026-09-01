using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Drawings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Drawings.Queries;

public sealed class ListDrawingFoldersForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListDrawingFoldersForProject, IReadOnlyList<DrawingFolder>> handler;

    public ListDrawingFoldersForProjectEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListDrawingFoldersForProject, IReadOnlyList<DrawingFolder>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Folders are part of reading the register, so the read gate matches ListDrawingsForProject.
    private static readonly RoleSet RolesThatMayReadDrawings = JpmsRoleSets.DrawingReaders;

    [Function(nameof(ListDrawingFoldersForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/drawing-folders")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadDrawings.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var folders = await handler.HandleAsync(
            new ListDrawingFoldersForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(folders);
    }
}
