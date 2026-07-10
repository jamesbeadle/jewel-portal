using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>POST /api/valuation-invoices/{valuationInvoiceId}/cancel — withdraw a Raised/Rejected invoice. Body: { note? }.</summary>
public sealed class CancelValuationInvoiceEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ValuationInvoiceWorkflowAuthorisation authorisation;
    private readonly ICommandHandler<CancelValuationInvoice, ValuationInvoice> handler;
    public CancelValuationInvoiceEndpoint(SignedInUserResolver users, ValuationInvoiceWorkflowAuthorisation authorisation, ICommandHandler<CancelValuationInvoice, ValuationInvoice> handler)
    { this.users = users; this.authorisation = authorisation; this.handler = handler; }

    [Function(nameof(CancelValuationInvoice))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "valuation-invoices/{valuationInvoiceId}/cancel")] HttpRequest request,
        string valuationInvoiceId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var body = await request.ReadFromJsonAsync<CancelValuationInvoice>();
        var command = new CancelValuationInvoice(valuationInvoiceId, body?.Note);
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
