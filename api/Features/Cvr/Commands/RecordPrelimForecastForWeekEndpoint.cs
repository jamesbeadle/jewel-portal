using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class RecordPrelimForecastForWeekEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RecordPrelimForecastForWeekAuthorisation authorisation;
    private readonly RecordPrelimForecastForWeekValidation validation;
    private readonly ICommandHandler<RecordPrelimForecastForWeek, PrelimForecastEntry> handler;

    public RecordPrelimForecastForWeekEndpoint(
        SignedInUserResolver users,
        RecordPrelimForecastForWeekAuthorisation authorisation,
        RecordPrelimForecastForWeekValidation validation,
        ICommandHandler<RecordPrelimForecastForWeek, PrelimForecastEntry> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RecordPrelimForecastForWeek))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/prelim-forecasts")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<RecordPrelimForecastForWeek>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var entry = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(entry);
    }
}
