using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// GET /api/xero/invoice/lines?id={invoiceId} — every stored line of one bill or credit note,
/// whatever tab each sits on, for the Invoice document window's bill card. Gated to the same
/// finance-facing roles as the rest of the allocation queue.
/// </summary>
public sealed class ListXeroLedgerLinesForInvoiceEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListXeroLedgerLinesForInvoice, IReadOnlyList<XeroLedgerLine>> handler;

    public ListXeroLedgerLinesForInvoiceEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListXeroLedgerLinesForInvoice, IReadOnlyList<XeroLedgerLine>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListXeroLedgerLinesForInvoice))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "xero/invoice/lines")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!XeroLedgerRoles.AllowedToAllocate.IncludesAny(signedInUser.Roles))
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var invoiceId = request.Query["id"].ToString();
        if (string.IsNullOrWhiteSpace(invoiceId)) return new BadRequestObjectResult("id is required.");

        var lines = await handler.HandleAsync(
            new ListXeroLedgerLinesForInvoice(invoiceId), request.HttpContext.RequestAborted);
        return new OkObjectResult(lines);
    }
}
