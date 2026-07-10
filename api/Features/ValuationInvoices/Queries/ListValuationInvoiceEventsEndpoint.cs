using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Queries;

public sealed class ListValuationInvoiceEventsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListValuationInvoiceEvents, IReadOnlyList<ValuationInvoiceEvent>> handler;
    public ListValuationInvoiceEventsEndpoint(SignedInUserResolver users, IQueryHandler<ListValuationInvoiceEvents, IReadOnlyList<ValuationInvoiceEvent>> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(ListValuationInvoiceEvents))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "valuation-invoices/{valuationInvoiceId}/events")] HttpRequest request,
        string valuationInvoiceId)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListValuationInvoiceEvents(valuationInvoiceId), request.HttpContext.RequestAborted));
    }
}
