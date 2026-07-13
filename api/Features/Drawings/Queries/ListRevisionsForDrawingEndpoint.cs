using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Drawings.Queries;

public sealed class ListRevisionsForDrawingEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListRevisionsForDrawing, IReadOnlyList<DrawingRevision>> handler;

    public ListRevisionsForDrawingEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListRevisionsForDrawing, IReadOnlyList<DrawingRevision>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Drawing reads span internal roles plus the externals who work from drawings (architect, subcontractor).
    private static readonly RoleSet RolesThatMayReadDrawings = JpmsRoleSets.DrawingReaders;

    [Function(nameof(ListRevisionsForDrawing))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "drawings/{drawingId}/revisions")] HttpRequest request,
        string drawingId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadDrawings.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        var status = Enum.TryParse<DrawingRevisionStatusFilter>(request.Query["status"], ignoreCase: true, out var parsed)
            ? parsed
            : DrawingRevisionStatusFilter.All;

        var revisions = await handler.HandleAsync(
            new ListRevisionsForDrawing(drawingId, status), request.HttpContext.RequestAborted);
        return new OkObjectResult(revisions);
    }
}
