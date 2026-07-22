using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

public sealed class CreateProgressReportEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateProgressReportAuthorisation authorisation;
    private readonly CreateProgressReportValidation validation;
    private readonly ICommandHandler<CreateProgressReport, ProgressReport> handler;

    public CreateProgressReportEndpoint(
        SignedInUserResolver users,
        CreateProgressReportAuthorisation authorisation,
        CreateProgressReportValidation validation,
        ICommandHandler<CreateProgressReport, ProgressReport> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CreateProgressReport))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/progress-reports")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<CreateProgressReport>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        // The creator is always the signed-in user; any client-supplied value is ignored.
        command = command with { CreatedByEmail = signedInUser.Email };

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var report = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(report);
    }
}
