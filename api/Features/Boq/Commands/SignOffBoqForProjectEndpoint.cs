using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Boq.Commands;

public sealed class SignOffBoqForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SignOffBoqForProjectAuthorisation authorisation;
    private readonly SignOffBoqForProjectValidation validation;
    private readonly ICommandHandler<SignOffBoqForProject, BoqSignOff> handler;

    public SignOffBoqForProjectEndpoint(
        SignedInUserResolver users,
        SignOffBoqForProjectAuthorisation authorisation,
        SignOffBoqForProjectValidation validation,
        ICommandHandler<SignOffBoqForProject, BoqSignOff> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SignOffBoqForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/boq/sign-off")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<SignOffBoqForProject>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = await validation.CheckAsync(command, request.HttpContext.RequestAborted);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var signOff = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(signOff);
    }
}
