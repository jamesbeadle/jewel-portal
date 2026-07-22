using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

public sealed class UpdateProgressUpdateEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateProgressUpdateAuthorisation authorisation;
    private readonly UpdateProgressUpdateValidation validation;
    private readonly ICommandHandler<UpdateProgressUpdate, ProgressUpdate> handler;

    public UpdateProgressUpdateEndpoint(
        SignedInUserResolver users,
        UpdateProgressUpdateAuthorisation authorisation,
        UpdateProgressUpdateValidation validation,
        ICommandHandler<UpdateProgressUpdate, ProgressUpdate> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UpdateProgressUpdate))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "progress-updates/{progressUpdateId}")] HttpRequest request,
        string progressUpdateId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<UpdateProgressUpdate>();
        if (command is null) return new BadRequestResult();
        if (command.ProgressUpdateId != progressUpdateId) return new BadRequestObjectResult("Route progressUpdateId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var update = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(update);
    }
}
