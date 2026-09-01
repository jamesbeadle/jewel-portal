using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

public sealed class UpdateWeeklyCashflowItemEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateWeeklyCashflowItemAuthorisation authorisation;
    private readonly UpdateWeeklyCashflowItemValidation validation;
    private readonly ICommandHandler<UpdateWeeklyCashflowItem, WeeklyCashflowItem> handler;

    public UpdateWeeklyCashflowItemEndpoint(
        SignedInUserResolver users,
        UpdateWeeklyCashflowItemAuthorisation authorisation,
        UpdateWeeklyCashflowItemValidation validation,
        ICommandHandler<UpdateWeeklyCashflowItem, WeeklyCashflowItem> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UpdateWeeklyCashflowItem))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "weekly-cashflow/items/{weeklyCashflowItemId}")] HttpRequest request,
        string weeklyCashflowItemId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<UpdateWeeklyCashflowItem>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("An item body is required.");
        var command = posted with { WeeklyCashflowItemId = weeklyCashflowItemId };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
