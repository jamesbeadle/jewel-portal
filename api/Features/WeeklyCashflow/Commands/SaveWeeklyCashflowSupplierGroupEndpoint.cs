using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

public sealed class SaveWeeklyCashflowSupplierGroupEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SaveWeeklyCashflowSupplierGroupAuthorisation authorisation;
    private readonly SaveWeeklyCashflowSupplierGroupValidation validation;
    private readonly ICommandHandler<SaveWeeklyCashflowSupplierGroup, WeeklyCashflowSupplierGroup> handler;

    public SaveWeeklyCashflowSupplierGroupEndpoint(
        SignedInUserResolver users,
        SaveWeeklyCashflowSupplierGroupAuthorisation authorisation,
        SaveWeeklyCashflowSupplierGroupValidation validation,
        ICommandHandler<SaveWeeklyCashflowSupplierGroup, WeeklyCashflowSupplierGroup> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SaveWeeklyCashflowSupplierGroup))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "weekly-cashflow/supplier-groups")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<SaveWeeklyCashflowSupplierGroup>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("A supplier group body is required.");
        var command = posted with { SavedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
