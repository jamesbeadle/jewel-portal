using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class RenameTradeEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RenameTradeAuthorisation authorisation;
    private readonly RenameTradeValidation validation;
    private readonly ICommandHandler<RenameTrade, Trade> handler;

    public RenameTradeEndpoint(SignedInUserResolver users, RenameTradeAuthorisation authorisation, RenameTradeValidation validation, ICommandHandler<RenameTrade, Trade> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(RenameTrade))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "trades/{tradeId}")] HttpRequest request,
        string tradeId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<RenameTrade>();
        if (command is null) return new BadRequestResult();
        if (command.TradeId != tradeId) return new BadRequestObjectResult("Route tradeId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (RenameTradeRefusedException refusal)
        {
            // 409: HttpCommandSender shows the message in the dialog without raising the toast.
            return new ConflictObjectResult(refusal.Message);
        }
    }
}
