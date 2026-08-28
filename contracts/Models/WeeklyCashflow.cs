namespace Jewel.JPMS.Models;

// ============================================================================
// The Weekly Cashflow — the accountant's live payment plan (Financial Reports).
//
// The Cash Forecast next door answers "does the company stay above water,
// month by month, to completion?" — a rough forward position. This is the
// other instrument: the NEXT 13 WEEKS, week by week, as the money will
// actually be paid. Xero seeds it (every outstanding supplier bill at its due
// week, every outstanding sales invoice at its due week); the accountant then
// MOVES entries to the week he will really pay them — cash is limited, and
// the order of payment is his call, not the due dates'. On top of the Xero
// rows he keeps the outgoings Xero can't see coming as manual items:
// subcontractors, staff, subscriptions — anything that isn't a project cost.
//
// Two kinds of record, both company-wide (no project scope):
//   * WeeklyCashflowItem — a manual outgoing, one-off or recurring.
//   * WeeklyCashflowPlacement — "this entry is planned for THAT week", keyed
//     by a stable per-entry string, so a moved bill stays moved for every
//     colleague and every reload.
// The arithmetic that turns these plus the Xero snapshots into the weekly
// grid is WeeklyCashflowMaths (contracts/WeeklyCashflow) — pure, unit-tested.
// ============================================================================

/// <summary>What kind of manual outgoing an item is — drives the band it renders under.
/// Persisted as its integer value (WeeklyCashflowItems.Category), so new members are APPENDED
/// here and never inserted mid-list.</summary>
public enum WeeklyCashflowCategory
{
    Subcontractor = 0,
    Staff = 1,
    Subscription = 2,
    Other = 3,
    DirectDebit = 4
}

public static class WeeklyCashflowCategories
{
    public static readonly WeeklyCashflowCategory[] All =
    {
        WeeklyCashflowCategory.Subcontractor,
        WeeklyCashflowCategory.Staff,
        WeeklyCashflowCategory.Subscription,
        WeeklyCashflowCategory.DirectDebit,
        WeeklyCashflowCategory.Other
    };

    public static string Label(WeeklyCashflowCategory category) => category switch
    {
        WeeklyCashflowCategory.Subcontractor => "Subcontractor",
        WeeklyCashflowCategory.Staff => "Staff",
        WeeklyCashflowCategory.Subscription => "Subscription",
        WeeklyCashflowCategory.DirectDebit => "Direct debit",
        WeeklyCashflowCategory.Other => "Other",
        _ => category.ToString()
    };

    /// <summary>The band heading a category's items render under — the plural of the label.</summary>
    public static string BandLabel(WeeklyCashflowCategory category) => category switch
    {
        WeeklyCashflowCategory.Subcontractor => "Subcontractors",
        WeeklyCashflowCategory.Staff => "Staff",
        WeeklyCashflowCategory.Subscription => "Subscriptions",
        WeeklyCashflowCategory.DirectDebit => "Direct debits",
        WeeklyCashflowCategory.Other => "Other outgoings",
        _ => category.ToString()
    };
}

/// <summary>How often a manual item recurs. Persisted as its integer value
/// (WeeklyCashflowItems.Recurrence) — append, never insert.</summary>
public enum WeeklyCashflowRecurrence
{
    OneOff = 0,
    Weekly = 1,
    Monthly = 2
}

public static class WeeklyCashflowRecurrences
{
    public static readonly WeeklyCashflowRecurrence[] All =
    {
        WeeklyCashflowRecurrence.OneOff,
        WeeklyCashflowRecurrence.Weekly,
        WeeklyCashflowRecurrence.Monthly
    };

    public static string Label(WeeklyCashflowRecurrence recurrence) => recurrence switch
    {
        WeeklyCashflowRecurrence.OneOff => "One-off",
        WeeklyCashflowRecurrence.Weekly => "Weekly",
        WeeklyCashflowRecurrence.Monthly => "Monthly",
        _ => recurrence.ToString()
    };
}

/// <summary>
/// One manual outgoing on the Weekly Cashflow — money leaving that Xero doesn't yet know about:
/// a subcontractor's agreed payments, wages, a subscription, any non-project cost.
///
/// FirstDueOn is a UK-local calendar date at midnight UTC (the SiteClock rule): the one-off's
/// due date, or the first occurrence a recurring item runs from — Weekly repeats on that
/// weekday, Monthly on that day of the month (clamped to shorter months). LastDueOn (inclusive)
/// ends a recurring item; null = open-ended. Amount is per occurrence, always positive — the
/// item IS cash out.
///
/// Archiving retires an item from the grid without losing who added what when — there is no
/// hard delete, matching the paper trail everywhere else in the portal.
/// </summary>
public sealed record WeeklyCashflowItem(
    string WeeklyCashflowItemId,
    string Name,
    WeeklyCashflowCategory Category,
    decimal Amount,
    WeeklyCashflowRecurrence Recurrence,
    DateTimeOffset FirstDueOn,
    DateTimeOffset? LastDueOn,
    string? Notes,
    string CreatedByEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ArchivedAt);

/// <summary>
/// The accountant's word over a due date: "I am paying this entry in THAT week." Keyed by the
/// entry's stable placement key — "bill:{XeroInvoiceId}", "receipt:{XeroInvoiceId}" or
/// "manual:{WeeklyCashflowItemId}:{yyyy-MM-dd}" (a recurring item's single occurrence, keyed by
/// its natural date so each week's payment moves independently). PlannedWeekStart is the target
/// week's Monday, midnight UTC. One row per entry, newest word wins; clearing the placement
/// returns the entry to its natural week.
/// </summary>
public sealed record WeeklyCashflowPlacement(
    string PlacementKey,
    DateTimeOffset PlannedWeekStart,
    string MovedByEmail,
    DateTimeOffset MovedAt);

/// <summary>
/// A set of Xero supplier (contact) names the Supplier bills band pulls together into ONE line —
/// e.g. "Materials" for Grant &amp; Stone, HSS Hire and Skip IT. Company-wide and purely an
/// arrangement of the rows: every bill keeps its own placement key, so moves keep working per
/// bill, and dissolving a group changes nothing but the reading order. Matching is by the
/// bill's Xero contact name, case-insensitive, as the aged payables read reports it.
/// </summary>
public sealed record WeeklyCashflowSupplierGroup(
    string SupplierGroupId,
    string Name,
    IReadOnlyList<string> ContactNames,
    string CreatedByEmail,
    DateTimeOffset CreatedAt);

/// <summary>
/// The accountant's "don't count this one": the Xero-fed entry behind PlacementKey is excluded
/// from the plan's arithmetic — typically a bill whose money already goes out through a manual
/// direct-debit item, so counting both would double it. The entry stays visible in the band's
/// Excluded fold (nothing silently disappears) and is restored by lifting the exclusion. One
/// row per entry, stamped with who and when.
/// </summary>
public sealed record WeeklyCashflowExclusion(
    string PlacementKey,
    string ExcludedByEmail,
    DateTimeOffset ExcludedAt);

/// <summary>Everything the Weekly Cashflow page stores of its own — the manual items (archived
/// ones excluded), every placement, the supplier groups and the exclusions. The Xero side of
/// the grid comes from the aged payables / receivables snapshots the page already reads.</summary>
public sealed record WeeklyCashflowPlan(
    IReadOnlyList<WeeklyCashflowItem> Items,
    IReadOnlyList<WeeklyCashflowPlacement> Placements,
    IReadOnlyList<WeeklyCashflowSupplierGroup> SupplierGroups,
    IReadOnlyList<WeeklyCashflowExclusion> Exclusions);
