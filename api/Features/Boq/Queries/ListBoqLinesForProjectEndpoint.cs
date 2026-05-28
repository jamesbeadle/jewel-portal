using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

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

    [Function(nameof(ListBoqLinesForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/boq")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var lines = await handler.HandleAsync(new ListBoqLinesForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(lines);
    }
}
