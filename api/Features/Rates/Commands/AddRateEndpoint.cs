using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Rates;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Rates.Commands;

public sealed class AddRateEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AddRateAuthorisation authorisation;
    private readonly AddRateValidation validation;
    private readonly ICommandHandler<AddRate, Rate> handler;

    public AddRateEndpoint(
        SignedInUserResolver users,
        AddRateAuthorisation authorisation,
        AddRateValidation validation,
        ICommandHandler<AddRate, Rate> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(AddRate))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "rates")] HttpRequest request)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<AddRate>();
        if (command is null) return new BadRequestResult();

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var rate = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(rate);
    }
}
