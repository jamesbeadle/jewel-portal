using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

public sealed class RegisterDrawingEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RegisterDrawingAuthorisation authorisation;
    private readonly RegisterDrawingValidation validation;
    private readonly ICommandHandler<RegisterDrawing, Drawing> handler;

    public RegisterDrawingEndpoint(
        SignedInUserResolver users,
        RegisterDrawingAuthorisation authorisation,
        RegisterDrawingValidation validation,
        ICommandHandler<RegisterDrawing, Drawing> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RegisterDrawing))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/drawings")] HttpRequest request,
        string projectId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<RegisterDrawing>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var drawing = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(drawing);
    }
}
