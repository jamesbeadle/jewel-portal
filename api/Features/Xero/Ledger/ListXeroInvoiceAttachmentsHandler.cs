using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// Lists the attachments Xero holds for one invoice or credit note, live from Xero
/// (nothing stored in JPMS). Xero refusals surface as InvalidOperationException so
/// the endpoint can return the message verbatim — e.g. a missing
/// accounting.attachments scope on the custom connection.
/// </summary>
public sealed class ListXeroInvoiceAttachmentsHandler
    : IQueryHandler<ListXeroInvoiceAttachments, IReadOnlyList<XeroInvoiceAttachment>>
{
    private readonly IXeroClient xero;

    public ListXeroInvoiceAttachmentsHandler(IXeroClient xero) { this.xero = xero; }

    public async Task<IReadOnlyList<XeroInvoiceAttachment>> HandleAsync(
        ListXeroInvoiceAttachments query, CancellationToken cancellationToken)
    {
        try
        {
            return await xero.ListAttachmentsAsync(query.XeroInvoiceId, query.IsCreditNote, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException(ex.Message);
        }
    }
}
