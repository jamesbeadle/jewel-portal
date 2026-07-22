using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>POST /api/valuation-invoices/{valuationInvoiceId}/approve — record the client's approval. Body: { note? }.</summary>
public sealed class ApproveValuationInvoiceEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ValuationInvoiceWorkflowAuthorisation authorisation;
    private readonly ICommandHandler<ApproveValuationInvoice, ValuationInvoice> handler;
    public ApproveValuationInvoiceEndpoint(SignedInUserResolver users, ValuationInvoiceWorkflowAuthorisation authorisation, ICommandHandler<ApproveValuationInvoice, ValuationInvoice> handler)
    { this.users = users; this.authorisation = authorisation; this.handler = handler; }

    [Function(nameof(ApproveValuationInvoice))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "valuation-invoices/{valuationInvoiceId}/approve")] HttpRequest request,
        string valuationInvoiceId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var body = await request.ReadFromJsonAsync<ApproveValuationInvoice>();
        var command = new ApproveValuationInvoice(valuationInvoiceId, body?.Note);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
