using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Drawings.Queries;

public sealed class GetDrawingByIdEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetDrawingById, Drawing?> handler;

    public GetDrawingByIdEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetDrawingById, Drawing?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetDrawingById))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "drawings/{drawingId}")] HttpRequest request,
        string drawingId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var drawing = await handler.HandleAsync(new GetDrawingById(drawingId), request.HttpContext.RequestAborted);
        return new OkObjectResult(drawing);
    }
}
