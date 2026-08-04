using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Xero;

// ============================================================================
// Xero site P&L — the accounts' own monthly income and cost per job.
//
// Xero holds a "Sites" tracking category whose options map one-to-one onto
// projects (ProjectEntity.XeroSiteName). Filtering Xero's profit & loss report
// by a site option gives the job's own P&L: everything invoiced to the client
// (sales tracked to the site) and everything spent (cost of sales tracked to
// the site), month by month, exactly as the accountant reads it in Xero. The
// sync stores those monthly figures in JPMS so the Profit Summary's cumulative
// invoiced-vs-cost chart is a database read, not a live Xero call — the
// nightly worker refreshes them, and the page's Refresh button re-pulls on
// demand.
//
// The figures report the INVOICED rung of the ladder (claimed → certified →
// invoiced → paid), not the certified rung the Profit Summary table reports —
// they will not reconcile to the penny with certified value, and retention is
// only present where it is tracked to the site in Xero.
// ============================================================================

/// <summary>The stored monthly site P&L for every project, oldest month first per project.</summary>
public sealed record GetXeroSitePnl : IQuery<XeroSitePnlSnapshot>;

/// <summary>
/// What JPMS holds from the last site P&L sync. <see cref="IsConfigured"/> is false when the
/// Xero client id/secret app settings are missing (the UI explains rather than erroring);
/// <see cref="LastSyncedAtUtc"/> is null until the first sync lands.
/// </summary>
public sealed record XeroSitePnlSnapshot(
    bool IsConfigured,
    DateTimeOffset? LastSyncedAtUtc,
    IReadOnlyList<XeroSiteMonthlyPnl> Rows)
{
    public static XeroSitePnlSnapshot Empty(bool isConfigured) =>
        new(isConfigured, null, Array.Empty<XeroSiteMonthlyPnl>());
}

/// <summary>
/// One month of one project's site P&L as Xero reports it. <see cref="Month"/> is the first
/// day of the month; amounts are the month's own movement (not cumulative) in the
/// organisation's base currency. Income is the report's income/turnover total, CostOfSales
/// its cost-of-sales/direct-costs total, OperatingExpenses its operating-expenses total
/// (rarely tracked to a site, stored for completeness).
/// </summary>
public sealed record XeroSiteMonthlyPnl(
    string ProjectId,
    DateTime Month,
    decimal Income,
    decimal CostOfSales,
    decimal OperatingExpenses);

/// <summary>One month's figures as read off Xero's report, before a project is attached.</summary>
public sealed record XeroSitePnlMonthFigures(
    DateTime Month,
    decimal Income,
    decimal CostOfSales,
    decimal OperatingExpenses);

/// <summary>
/// Re-reads every mapped project's site P&L from Xero (profit &amp; loss report filtered by
/// the project's Sites tracking option, monthly columns from the reporting window's start)
/// and replaces the stored months. Projects without a Xero site mapping are skipped and
/// named in the result so the UI can say why a job has no line.
/// </summary>
public sealed record SyncXeroSitePnl : ICommand<XeroSitePnlSyncResult>;

public sealed record XeroSitePnlSyncResult(
    bool IsConfigured,
    string? Error,
    int ProjectsSynced,
    int MonthsStored,
    IReadOnlyList<string> UnmappedProjectNames);
