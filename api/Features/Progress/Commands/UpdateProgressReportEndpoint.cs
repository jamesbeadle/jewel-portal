using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

public sealed class UpdateProgressReportEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateProgressReportAuthorisation authorisation;
    private readonly UpdateProgressReportValidation validation;
    private readonly ICommandHandler<UpdateProgressReport, ProgressReport> handler;

    public UpdateProgressReportEndpoint(
        SignedInUserResolver users,
        UpdateProgressReportAuthorisation authorisation,
        UpdateProgressReportValidation validation,
        ICommandHandler<UpdateProgressReport, ProgressReport> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UpdateProgressReport))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "progress-reports/{progressReportId}")] HttpRequest request,
        string progressReportId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<UpdateProgressReport>();
        if (command is null) return new BadRequestResult();
        if (command.ProgressReportId != progressReportId) return new BadRequestObjectResult("Route progressReportId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var report = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(report);
    }
}
