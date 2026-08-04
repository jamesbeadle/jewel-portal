using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One month of one project's site P&amp;L as Xero reports it: the profit &amp; loss report
/// filtered by the project's "Sites" tracking option, monthly columns. Keyed on
/// "{ProjectId}:{yyyy-MM}" so syncs upsert deterministically; every field is Xero-owned and
/// refreshed on each sync (nothing here is allocated or edited in JPMS). Amounts are the
/// month's own movement, not cumulative — the Profit Summary accumulates client-side.
/// Months whose figures come back all-zero are removed rather than stored, so a recode in
/// Xero (a line moved off a site) disappears here on the next sync.
/// </summary>
public sealed class XeroSitePnlMonthEntity
{
    [Key, MaxLength(80)] public string XeroSitePnlMonthId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";

    /// <summary>First day of the month the figures cover.</summary>
    public DateTime Month { get; set; }

    /// <summary>The report's income/turnover total for the month (sales invoiced to the site).</summary>
    public decimal Income { get; set; }

    /// <summary>The report's cost-of-sales / direct-costs total for the month.</summary>
    public decimal CostOfSales { get; set; }

    /// <summary>The report's operating-expenses total — rarely tracked to a site; stored for completeness.</summary>
    public decimal OperatingExpenses { get; set; }

    public DateTimeOffset LastSyncedAtUtc { get; set; }
}
