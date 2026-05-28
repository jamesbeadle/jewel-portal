using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Rates;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Rates.Commands;

public sealed class ReviseRateEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ReviseRateAuthorisation authorisation;
    private readonly ReviseRateValidation validation;
    private readonly ICommandHandler<ReviseRate, Rate> handler;

    public ReviseRateEndpoint(
        SignedInUserResolver users,
        ReviseRateAuthorisation authorisation,
        ReviseRateValidation validation,
        ICommandHandler<ReviseRate, Rate> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(ReviseRate))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "rates/{rateId}")] HttpRequest request,
        string rateId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<ReviseRate>();
        if (command is null) return new BadRequestResult();
        if (command.RateId != rateId) return new BadRequestObjectResult("Route rateId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var rate = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(rate);
    }
}
