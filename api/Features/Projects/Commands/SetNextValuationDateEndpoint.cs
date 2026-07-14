using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

public sealed class SetNextValuationDateEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetNextValuationDateAuthorisation authorisation;
    private readonly ICommandHandler<SetNextValuationDate, Project> handler;

    public SetNextValuationDateEndpoint(
        SignedInUserResolver users,
        SetNextValuationDateAuthorisation authorisation,
        ICommandHandler<SetNextValuationDate, Project> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.handler = handler;
    }

    [Function(nameof(SetNextValuationDate))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "projects/{projectId}/next-valuation-date")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<SetNextValuationDate>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var project = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(project);
    }
}
