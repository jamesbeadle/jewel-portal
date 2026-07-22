using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>POST /api/valuation-invoices/{valuationInvoiceId}/payment — record the client's payment. Body: { amountPaid }.</summary>
public sealed class RecordValuationInvoicePaymentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RecordValuationInvoicePaymentAuthorisation authorisation;
    private readonly RecordValuationInvoicePaymentValidation validation;
    private readonly ICommandHandler<RecordValuationInvoicePayment, ValuationInvoice> handler;

    public RecordValuationInvoicePaymentEndpoint(
        SignedInUserResolver users,
        RecordValuationInvoicePaymentAuthorisation authorisation,
        RecordValuationInvoicePaymentValidation validation,
        ICommandHandler<RecordValuationInvoicePayment, ValuationInvoice> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RecordValuationInvoicePayment))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "valuation-invoices/{valuationInvoiceId}/payment")] HttpRequest request,
        string valuationInvoiceId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<RecordValuationInvoicePayment>();
        if (body is null) return new BadRequestResult();

        var command = body with { ValuationInvoiceId = valuationInvoiceId };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
