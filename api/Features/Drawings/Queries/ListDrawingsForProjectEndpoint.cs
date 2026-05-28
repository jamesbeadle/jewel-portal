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

    [Function(nameof(ListDrawingsForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/drawings")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var drawings = await handler.HandleAsync(new ListDrawingsForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(drawings);
    }
}
