using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

public sealed class CreateProjectShellEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateProjectShellAuthorisation authorisation;
    private readonly CreateProjectShellValidation validation;
    private readonly ICommandHandler<CreateProjectShell, Project> handler;

    public CreateProjectShellEndpoint(
        SignedInUserResolver users,
        CreateProjectShellAuthorisation authorisation,
        CreateProjectShellValidation validation,
        ICommandHandler<CreateProjectShell, Project> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CreateProjectShell))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects")] HttpRequest request)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<CreateProjectShell>();
        if (command is null) return new BadRequestResult();

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var project = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(project);
    }
}
