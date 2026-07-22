using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>
/// PUT /api/valuation-invoices/{valuationInvoiceId} — amend period/amount (and, for manual
/// invoices, paid amount and backdated dates). Body: { periodMonth, amount, amountPaid?, issuedAt?, paidAt?, note? }.
/// </summary>
public sealed class UpdateValuationInvoiceEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ValuationInvoiceWorkflowAuthorisation authorisation;
    private readonly UpdateValuationInvoiceValidation validation;
    private readonly ICommandHandler<UpdateValuationInvoice, ValuationInvoice> handler;
    public UpdateValuationInvoiceEndpoint(SignedInUserResolver users, ValuationInvoiceWorkflowAuthorisation authorisation, UpdateValuationInvoiceValidation validation, ICommandHandler<UpdateValuationInvoice, ValuationInvoice> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(UpdateValuationInvoice))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "valuation-invoices/{valuationInvoiceId}")] HttpRequest request,
        string valuationInvoiceId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var body = await request.ReadFromJsonAsync<UpdateValuationInvoice>();
        if (body is null) return new BadRequestResult();
        var command = body with { ValuationInvoiceId = valuationInvoiceId };
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
