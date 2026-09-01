using Jewel.JPMS.Contracts.Rates;

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
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<AddRate>();
        if (command is null) return new BadRequestResult();

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var rate = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(rate);
    }
}
