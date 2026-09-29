using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Queries;

public sealed class ListRevisionsForDrawingEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IQueryHandler<ListRevisionsForDrawing, IReadOnlyList<DrawingRevision>> handler;

    public ListRevisionsForDrawingEndpoint(
        SignedInUserResolver users,
        JpmsContext context,
        IQueryHandler<ListRevisionsForDrawing, IReadOnlyList<DrawingRevision>> handler)
    {
        this.users = users;
        this.context = context;
        this.handler = handler;
    }

    // Drawing reads span internal roles plus the externals who work from drawings (architect, subcontractor).
    private static readonly RoleSet RolesThatMayReadDrawings = JpmsRoleSets.DrawingReaders;

    [Function(nameof(ListRevisionsForDrawing))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "drawings/{drawingId}/revisions")] HttpRequest request,
        string drawingId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadDrawings.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!await DrawingScope.MayReadDrawingAsync(context, signedInUser, drawingId, cancellationToken))
            return new StatusCodeResult(403);

        var status = Enum.TryParse<DrawingRevisionStatusFilter>(request.Query["status"], ignoreCase: true, out var parsed)
            ? parsed
            : DrawingRevisionStatusFilter.All;

        var revisions = await handler.HandleAsync(
            new ListRevisionsForDrawing(drawingId, status), cancellationToken);
        return new OkObjectResult(revisions);
    }
}
