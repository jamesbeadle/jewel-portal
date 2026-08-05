using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Projects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

public sealed class DeleteProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DeleteProjectAuthorisation authorisation;
    private readonly DeleteProjectValidation validation;
    private readonly ICommandHandler<DeleteProject, Acknowledgement> handler;

    public DeleteProjectEndpoint(
        SignedInUserResolver users,
        DeleteProjectAuthorisation authorisation,
        DeleteProjectValidation validation,
        ICommandHandler<DeleteProject, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(DeleteProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "projects/{projectId}")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<DeleteProject>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            var acknowledgement = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
            return new OkObjectResult(acknowledgement);
        }
        catch (DeleteProjectRefusedException refusal)
        {
            // 409: HttpCommandSender shows the message in the dialog without raising the toast.
            return new ConflictObjectResult(refusal.Message);
        }
    }
}
