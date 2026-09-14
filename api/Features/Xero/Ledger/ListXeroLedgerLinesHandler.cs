using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

public sealed class ListXeroLedgerLinesHandler : IQueryHandler<ListXeroLedgerLines, IReadOnlyList<XeroLedgerLine>>
{
    private readonly JpmsContext context;

    public ListXeroLedgerLinesHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<XeroLedgerLine>> HandleAsync(ListXeroLedgerLines query, CancellationToken cancellationToken)
    {
        // Filter in SQL. The allocation page asks for one status at a time — it is a tab per
        // status — so this is the difference between reading the tab someone is looking at and
        // reading every line the business has ever received. (XeroLedgerLines already carries an
        // index on AllocationStatus.)
        var lines = context.XeroLedgerLines.AsNoTracking();
        if (query.Status is { } status)
            lines = lines.Where(line => line.AllocationStatus == (int)status);

        var entities = await lines
            .OrderByDescending(line => line.Date)
            .ToListAsync(cancellationToken);

        // Splits, suggestions, the dispute thread, labour and Work Order bill recognition and the
        // standing approval — the one enrichment every read of a line shares (XeroLedgerReads).
        return await XeroLedgerReads.ToModelsAsync(context, entities, cancellationToken);
    }
}
