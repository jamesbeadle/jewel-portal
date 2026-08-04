using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Xero.SitePnl;

/// <summary>
/// The stored site P&amp;L, straight from XeroSitePnlMonths — a database read, never a live
/// Xero call (the nightly worker and the explicit sync command own the refresh). Ordered per
/// project oldest month first, which is the order the cumulative chart accumulates in.
/// </summary>
public sealed class GetXeroSitePnlHandler : IQueryHandler<GetXeroSitePnl, XeroSitePnlSnapshot>
{
    private readonly JpmsContext context;
    private readonly IXeroClient xero;

    public GetXeroSitePnlHandler(JpmsContext context, IXeroClient xero)
    {
        this.context = context;
        this.xero = xero;
    }

    public async Task<XeroSitePnlSnapshot> HandleAsync(GetXeroSitePnl query, CancellationToken cancellationToken)
    {
        var rows = await context.XeroSitePnlMonths
            .OrderBy(row => row.ProjectId)
            .ThenBy(row => row.Month)
            .Select(row => new XeroSiteMonthlyPnl(
                row.ProjectId, row.Month, row.Income, row.CostOfSales, row.OperatingExpenses))
            .ToListAsync(cancellationToken);

        // Null until the first sync lands — the UI reads that as "never synced", not "empty".
        var lastSynced = await context.XeroSitePnlMonths
            .MaxAsync(row => (DateTimeOffset?)row.LastSyncedAtUtc, cancellationToken);

        return new XeroSitePnlSnapshot(xero.IsConfigured, lastSynced, rows);
    }
}
