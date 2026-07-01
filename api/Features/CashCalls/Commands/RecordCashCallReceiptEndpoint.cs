using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CashCalls;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.CashCalls.Commands;

/// <summary>POST /api/cash-calls/{cashCallId}/receipt — record the client's payment. Body: { amountReceived }.</summary>
public sealed class RecordCashCallReceiptEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RecordCashCallReceiptAuthorisation authorisation;
    private readonly RecordCashCallReceiptValidation validation;
    private readonly ICommandHandler<RecordCashCallReceipt, CashCall> handler;

    public RecordCashCallReceiptEndpoint(
        SignedInUserResolver users,
        RecordCashCallReceiptAuthorisation authorisation,
        RecordCashCallReceiptValidation validation,
        ICommandHandler<RecordCashCallReceipt, CashCall> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RecordCashCallReceipt))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cash-calls/{cashCallId}/receipt")] HttpRequest request,
        string cashCallId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<RecordCashCallReceipt>();
        if (body is null) return new BadRequestResult();

        var command = body with { CashCallId = cashCallId };

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
