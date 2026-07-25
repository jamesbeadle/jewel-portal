using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;

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

        var splitsByLine = await XeroLedgerReads.SplitsForAsync(context, entities, cancellationToken);
        var suggester = await XeroLedgerReads.SuggesterForAsync(context, entities, cancellationToken);

        return entities.Select(entity => XeroLedgerReads.ToModel(
            entity,
            splitsByLine.TryGetValue(entity.XeroLedgerLineId, out var splits) ? splits : null,
            suggester)).ToList();
    }
}
