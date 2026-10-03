using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Commands;

public sealed class KeyBankStatementBalanceEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly KeyBankStatementBalanceAuthorisation authorisation;
    private readonly KeyBankStatementBalanceValidation validation;
    private readonly ICommandHandler<KeyBankStatementBalance, KeyedBankStatementBalance> handler;

    public KeyBankStatementBalanceEndpoint(
        SignedInUserResolver users,
        KeyBankStatementBalanceAuthorisation authorisation,
        KeyBankStatementBalanceValidation validation,
        ICommandHandler<KeyBankStatementBalance, KeyedBankStatementBalance> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(KeyBankStatementBalance))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "xero/bank-statement-balances")] HttpRequest request)
    {
        var httpContext = request.HttpContext;
        var cancellationToken = httpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<KeyBankStatementBalance>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("A statement balance body is required.");
        var command = posted with { KeyedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
