using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Drawings.Queries;

public sealed class ListDrawingsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListDrawingsForProject, IReadOnlyList<Drawing>> handler;

    public ListDrawingsForProjectEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListDrawingsForProject, IReadOnlyList<Drawing>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Drawing reads span internal roles plus the externals who work from drawings (architect, subcontractor).
    private static readonly RoleSet RolesThatMayReadDrawings = JpmsRoleSets.DrawingReaders;

    [Function(nameof(ListDrawingsForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/drawings")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadDrawings.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var approvedOnly = string.Equals(request.Query["approvedOnly"], "true", StringComparison.OrdinalIgnoreCase);

        var drawings = await handler.HandleAsync(
            new ListDrawingsForProject(projectId, approvedOnly), request.HttpContext.RequestAborted);
        return new OkObjectResult(drawings);
    }
}
