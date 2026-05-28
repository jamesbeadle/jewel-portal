using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

public sealed class UpdateProjectDetailsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateProjectDetailsAuthorisation authorisation;
    private readonly UpdateProjectDetailsValidation validation;
    private readonly ICommandHandler<UpdateProjectDetails, Project> handler;

    public UpdateProjectDetailsEndpoint(
        SignedInUserResolver users,
        UpdateProjectDetailsAuthorisation authorisation,
        UpdateProjectDetailsValidation validation,
        ICommandHandler<UpdateProjectDetails, Project> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UpdateProjectDetails))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "projects/{projectId}")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<UpdateProjectDetails>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var project = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(project);
    }
}
