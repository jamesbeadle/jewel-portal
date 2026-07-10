using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

// ============================================================================
// The supplier's actual document, viewed from the allocation page. Xero holds
// the attachment Dext published with each bill; these endpoints list and stream
// it live from Xero — nothing is stored in JPMS. Both are gated to the same
// finance-facing roles as the rest of the allocation queue. Requires the Xero
// custom connection's accounting.attachments scope.
// ============================================================================

/// <summary>
/// GET /api/xero/invoice/attachments?id={invoiceId}&amp;credit=1 — lists the attachments Xero
/// holds for one purchase invoice (or credit note, with credit=1).
/// </summary>
public sealed class ListXeroInvoiceAttachmentsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListXeroInvoiceAttachments, IReadOnlyList<XeroInvoiceAttachment>> handler;

    public ListXeroInvoiceAttachmentsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListXeroInvoiceAttachments, IReadOnlyList<XeroInvoiceAttachment>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListXeroInvoiceAttachments))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "xero/invoice/attachments")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!XeroLedgerRoles.AllowedToAllocate.IncludesAny(signedInUser.Roles))
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var invoiceId = request.Query["id"].ToString();
        if (string.IsNullOrWhiteSpace(invoiceId)) return new BadRequestObjectResult("id is required.");
        var isCreditNote = IsTruthy(request.Query["credit"].ToString());

        try
        {
            var attachments = await handler.HandleAsync(
                new ListXeroInvoiceAttachments(invoiceId, isCreditNote), request.HttpContext.RequestAborted);
            return new OkObjectResult(attachments);
        }
        catch (InvalidOperationException ex)
        {
            // Bare string so HttpQueryClient/HttpCommandSender-style consumers surface it verbatim.
            return new BadRequestObjectResult(ex.Message);
        }
    }

    internal static bool IsTruthy(string? value) =>
        value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// GET /api/xero/invoice/attachment?id={invoiceId}&amp;file={fileName}&amp;credit=1&amp;inline=1 —
/// streams one attachment's bytes, proxied through the API on demand. Same inline/download split
/// as the drawing and mailbox file endpoints: inline requests come from the in-app preview iframe
/// (Content-Disposition unset so the browser renders in place); explicit downloads get a filename.
/// The file name travels in the query string, never the route path — supplier file names carry
/// spaces and other characters that don't survive a URL path segment.
/// </summary>
public sealed class DownloadXeroInvoiceAttachmentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IXeroClient xero;

    public DownloadXeroInvoiceAttachmentEndpoint(SignedInUserResolver users, IXeroClient xero)
    {
        this.users = users;
        this.xero = xero;
    }

    [Function(nameof(DownloadXeroInvoiceAttachmentEndpoint))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "xero/invoice/attachment")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!XeroLedgerRoles.AllowedToAllocate.IncludesAny(signedInUser.Roles))
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var invoiceId = request.Query["id"].ToString();
        var fileName = request.Query["file"].ToString();
        if (string.IsNullOrWhiteSpace(invoiceId) || string.IsNullOrWhiteSpace(fileName))
            return new BadRequestObjectResult("id and file are required.");
        var isCreditNote = ListXeroInvoiceAttachmentsEndpoint.IsTruthy(request.Query["credit"].ToString());

        XeroAttachmentContent? attachment;
        try
        {
            attachment = await xero.GetAttachmentAsync(invoiceId, isCreditNote, fileName, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new BadRequestObjectResult(ex.Message);
        }

        if (attachment is null)
            return new NotFoundObjectResult(
                "Couldn't fetch that document from Xero — it may have been removed from the invoice.");

        var inline = ListXeroInvoiceAttachmentsEndpoint.IsTruthy(request.Query["inline"].ToString());

        var result = new FileContentResult(attachment.Content, attachment.ContentType)
        {
            EnableRangeProcessing = true
        };
        if (!inline) result.FileDownloadName = attachment.FileName;
        return result;
    }
}
