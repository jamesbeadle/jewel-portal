using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.CommercialInputs.Commands;

public sealed class LogDayworkEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly LogDayworkAuthorisation authorisation;
    private readonly LogDayworkValidation validation;
    private readonly ICommandHandler<LogDaywork, Daywork> handler;

    public LogDayworkEndpoint(
        SignedInUserResolver users,
        LogDayworkAuthorisation authorisation,
        LogDayworkValidation validation,
        ICommandHandler<LogDaywork, Daywork> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(LogDaywork))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/dayworks")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<LogDaywork>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var daywork = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(daywork);
    }
}
