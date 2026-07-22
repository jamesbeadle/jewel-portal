using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class UpdateProgrammeTaskEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateProgrammeTaskAuthorisation authorisation;
    private readonly UpdateProgrammeTaskValidation validation;
    private readonly ICommandHandler<UpdateProgrammeTask, ProgrammeTask> handler;
    public UpdateProgrammeTaskEndpoint(SignedInUserResolver users, UpdateProgrammeTaskAuthorisation authorisation, UpdateProgrammeTaskValidation validation, ICommandHandler<UpdateProgrammeTask, ProgrammeTask> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(UpdateProgrammeTask))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "programme-tasks/{programmeTaskId}")] HttpRequest request, string programmeTaskId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<UpdateProgrammeTask>();
        if (command is null) return new BadRequestResult();
        if (command.ProgrammeTaskId != programmeTaskId) return new BadRequestObjectResult("Route programmeTaskId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
