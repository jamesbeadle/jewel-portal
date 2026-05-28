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

    [Function(nameof(ListRevisionsForDrawing))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "drawings/{drawingId}/revisions")] HttpRequest request,
        string drawingId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();

        var revisions = await handler.HandleAsync(new ListRevisionsForDrawing(drawingId), request.HttpContext.RequestAborted);
        return new OkObjectResult(revisions);
    }
}
