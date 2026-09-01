using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// The allocated lines coded to one project, newest first, capped. The labour tab's
/// "mark invoice lines as covered" panel used to pull the entire company ledger and then keep the
/// hundred rows it wanted — the filter and the cap belong here, next to the index that serves them
/// (XeroLedgerLines is indexed on ProjectId + CostCenterCode).
/// </summary>
public sealed class ListXeroLedgerLinesForProjectHandler
    : IQueryHandler<ListXeroLedgerLinesForProject, IReadOnlyList<XeroLedgerLine>>
{
    private const int MaxTake = 500;

    private readonly JpmsContext context;

    public ListXeroLedgerLinesForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<XeroLedgerLine>> HandleAsync(
        ListXeroLedgerLinesForProject query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.ProjectId)) return Array.Empty<XeroLedgerLine>();

        var take = Math.Clamp(query.Take, 1, MaxTake);
        var entities = await context.XeroLedgerLines.AsNoTracking()
            .Where(line => line.ProjectId == query.ProjectId
                           && line.AllocationStatus == (int)XeroAllocationStatus.Allocated)
            .OrderByDescending(line => line.Date)
            .Take(take)
            .ToListAsync(cancellationToken);

        var splitsByLine = await XeroLedgerReads.SplitsForAsync(context, entities, cancellationToken);

        // Every row here is Allocated, so no suggester is built.
        return entities.Select(entity => XeroLedgerReads.ToModel(
            entity,
            splitsByLine.TryGetValue(entity.XeroLedgerLineId, out var splits) ? splits : null,
            suggester: null)).ToList();
    }
}
