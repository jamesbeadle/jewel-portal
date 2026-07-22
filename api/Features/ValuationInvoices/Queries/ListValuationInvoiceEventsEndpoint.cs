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

    // Valuation invoice reads are internal-only; external portal logins have no view of project money.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(ListValuationInvoiceEvents))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "valuation-invoices/{valuationInvoiceId}/events")] HttpRequest request,
        string valuationInvoiceId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListValuationInvoiceEvents(valuationInvoiceId), request.HttpContext.RequestAborted));
    }
}
