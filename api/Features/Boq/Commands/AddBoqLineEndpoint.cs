using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Boq.Commands;

public sealed class AddBoqLineEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AddBoqLineAuthorisation authorisation;
    private readonly AddBoqLineValidation validation;
    private readonly ICommandHandler<AddBoqLine, BoqLineItem> handler;

    public AddBoqLineEndpoint(
        SignedInUserResolver users,
        AddBoqLineAuthorisation authorisation,
        AddBoqLineValidation validation,
        ICommandHandler<AddBoqLine, BoqLineItem> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(AddBoqLine))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/boq")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<AddBoqLine>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var line = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(line);
    }
}
