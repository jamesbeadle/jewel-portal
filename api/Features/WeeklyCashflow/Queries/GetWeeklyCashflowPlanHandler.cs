using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.WeeklyCashflow;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Queries;

/// <summary>The plan in one read: live items (archived excluded — they've left the grid) plus
/// every placement. Placements are not pruned against the items or the Xero snapshots here — a
/// placement whose entry has gone (bill paid, item archived) is simply never asked for by the
/// grid, and deleting it would erase who planned what for nothing.</summary>
public sealed class GetWeeklyCashflowPlanHandler : IQueryHandler<GetWeeklyCashflowPlan, WeeklyCashflowPlan>
{
    private readonly JpmsContext context;

    public GetWeeklyCashflowPlanHandler(JpmsContext context) { this.context = context; }

    public async Task<WeeklyCashflowPlan> HandleAsync(GetWeeklyCashflowPlan query, CancellationToken cancellationToken)
    {
        var items = await context.WeeklyCashflowItems.AsNoTracking()
            .Where(item => item.ArchivedAt == null)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);

        var placements = await context.WeeklyCashflowPlacements.AsNoTracking()
            .ToListAsync(cancellationToken);

        var groups = await context.WeeklyCashflowSupplierGroups.AsNoTracking()
            .OrderBy(group => group.Name)
            .ToListAsync(cancellationToken);

        var exclusions = await context.WeeklyCashflowExclusions.AsNoTracking()
            .ToListAsync(cancellationToken);

        return new WeeklyCashflowPlan(
            items.Select(item => item.ToModel()).ToList(),
            placements.Select(placement => placement.ToModel()).ToList(),
            groups.Select(group => group.ToModel()).ToList(),
            exclusions.Select(exclusion => exclusion.ToModel()).ToList());
    }
}
