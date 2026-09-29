using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Queries;

public sealed class GetDrawingByIdEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IQueryHandler<GetDrawingById, Drawing?> handler;

    public GetDrawingByIdEndpoint(
        SignedInUserResolver users,
        JpmsContext context,
        IQueryHandler<GetDrawingById, Drawing?> handler)
    {
        this.users = users;
        this.context = context;
        this.handler = handler;
    }

    // Drawing reads span internal roles plus the externals who work from drawings (architect, subcontractor).
    private static readonly RoleSet RolesThatMayReadDrawings = JpmsRoleSets.DrawingReaders;

    [Function(nameof(GetDrawingById))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "drawings/{drawingId}")] HttpRequest request,
        string drawingId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadDrawings.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!await DrawingScope.MayReadDrawingAsync(context, signedInUser, drawingId, cancellationToken))
            return new StatusCodeResult(403);

        var drawing = await handler.HandleAsync(new GetDrawingById(drawingId), cancellationToken);
        return new OkObjectResult(drawing);
    }
}
