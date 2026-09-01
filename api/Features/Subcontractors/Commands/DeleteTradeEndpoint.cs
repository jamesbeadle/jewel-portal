using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class DeleteTradeEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DeleteTradeAuthorisation authorisation;
    private readonly DeleteTradeValidation validation;
    private readonly ICommandHandler<DeleteTrade, Acknowledgement> handler;

    public DeleteTradeEndpoint(SignedInUserResolver users, DeleteTradeAuthorisation authorisation, DeleteTradeValidation validation, ICommandHandler<DeleteTrade, Acknowledgement> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(DeleteTrade))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "trades/{tradeId}")] HttpRequest request,
        string tradeId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<DeleteTrade>();
        if (command is null) return new BadRequestResult();
        if (command.TradeId != tradeId) return new BadRequestObjectResult("Route tradeId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (DeleteTradeRefusedException refusal)
        {
            // 409: HttpCommandSender shows the message in the dialog without raising the toast.
            return new ConflictObjectResult(refusal.Message);
        }
    }
}
