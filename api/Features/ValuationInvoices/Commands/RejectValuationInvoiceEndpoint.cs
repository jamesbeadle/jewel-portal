using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>POST /api/valuation-invoices/{valuationInvoiceId}/reject — record the client's rejection. Body: { reason }.</summary>
public sealed class RejectValuationInvoiceEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ValuationInvoiceWorkflowAuthorisation authorisation;
    private readonly RejectValuationInvoiceValidation validation;
    private readonly ICommandHandler<RejectValuationInvoice, ValuationInvoice> handler;
    public RejectValuationInvoiceEndpoint(SignedInUserResolver users, ValuationInvoiceWorkflowAuthorisation authorisation, RejectValuationInvoiceValidation validation, ICommandHandler<RejectValuationInvoice, ValuationInvoice> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(RejectValuationInvoice))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "valuation-invoices/{valuationInvoiceId}/reject")] HttpRequest request,
        string valuationInvoiceId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var body = await request.ReadFromJsonAsync<RejectValuationInvoice>();
        if (body is null) return new BadRequestResult();
        var command = body with { ValuationInvoiceId = valuationInvoiceId };
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
