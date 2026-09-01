using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// One GROUP BY for the allocation page's tab bar. The page shows a count against every status but
/// only holds one status' lines at a time, so the counts can't come from counting what was
/// downloaded any more — and shouldn't have, since that meant downloading the whole ledger to
/// render four numbers.
/// </summary>
public sealed class GetXeroLedgerCountsHandler : IQueryHandler<GetXeroLedgerCounts, XeroLedgerCounts>
{
    private readonly JpmsContext context;

    public GetXeroLedgerCountsHandler(JpmsContext context) { this.context = context; }

    public async Task<XeroLedgerCounts> HandleAsync(GetXeroLedgerCounts query, CancellationToken cancellationToken)
    {
        var byStatus = await context.XeroLedgerLines.AsNoTracking()
            .GroupBy(line => line.AllocationStatus)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.Status, row => row.Count, cancellationToken);

        int CountOf(XeroAllocationStatus status) =>
            byStatus.TryGetValue((int)status, out var count) ? count : 0;

        return new XeroLedgerCounts(
            Unallocated: CountOf(XeroAllocationStatus.Unallocated),
            Allocated:   CountOf(XeroAllocationStatus.Allocated),
            Bucketed:    CountOf(XeroAllocationStatus.Bucketed),
            Ignored:     CountOf(XeroAllocationStatus.Ignored),
            Disputed:    CountOf(XeroAllocationStatus.Disputed));
    }
}
