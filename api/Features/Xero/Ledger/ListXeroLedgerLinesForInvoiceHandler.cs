using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// The stored lines of one bill, largest first, whatever tab each sits on. The Invoice document
/// window opens on ONE line; this is the rest of the bill beside it, so a note the bookkeeper put
/// on another line (or in the bill's Xero reference) is read without leaving the window. Stored
/// lines only — the cost-of-sales lines the sync keeps — so an overhead line on the same bill is
/// not here, and the nets do not claim to be the bill's total. Each line is shaped exactly as the
/// status read shapes it (queue, labour, Work Order bill, approval), so the connector reading a
/// bill whole sees the same tab the page would show every line in.
/// </summary>
public sealed class ListXeroLedgerLinesForInvoiceHandler
    : IQueryHandler<ListXeroLedgerLinesForInvoice, IReadOnlyList<XeroLedgerLine>>
{
    private readonly JpmsContext context;

    public ListXeroLedgerLinesForInvoiceHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<XeroLedgerLine>> HandleAsync(
        ListXeroLedgerLinesForInvoice query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.XeroInvoiceId)) return Array.Empty<XeroLedgerLine>();

        var entities = await context.XeroLedgerLines.AsNoTracking()
            .Where(line => line.XeroInvoiceId == query.XeroInvoiceId)
            .OrderByDescending(line => line.Net)
            .ThenBy(line => line.Description)
            .ToListAsync(cancellationToken);

        return await XeroLedgerReads.ToModelsAsync(context, entities, cancellationToken);
    }
}
