using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Projects.Queries;

public sealed class GetProjectByIdEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetProjectById, Project?> handler;

    public GetProjectByIdEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetProjectById, Project?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetProjectById))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}")] HttpRequest request,
        string projectId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();

        var project = await handler.HandleAsync(new GetProjectById(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(project);
    }
}
