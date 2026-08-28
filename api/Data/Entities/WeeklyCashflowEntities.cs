using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// ============================================================================
// The Weekly Cashflow's stored halves (docs/00-business-context — the
// accountant's live 13-week payment plan, Financial Reports). Everything else
// on that grid is read live from Xero (aged payables / receivables / the cash
// summary) and never stored here — same rule as the aged views themselves.
// Company-wide: no project scope on either table.
// ============================================================================

/// <summary>A manual outgoing — a subcontractor's payments, wages, a subscription: money leaving
/// that Xero doesn't yet hold a bill for. Category and Recurrence persist enum integer values
/// (WeeklyCashflowCategory / WeeklyCashflowRecurrence — append members, never insert). Archiving
/// is soft and stamped; archived items keep their rows (and their history) but leave the grid.</summary>
public sealed class WeeklyCashflowItemEntity
{
    [Key, MaxLength(64)] public string WeeklyCashflowItemId { get; set; } = "";
    [MaxLength(200)] public string Name { get; set; } = "";
    public int Category { get; set; }
    public decimal Amount { get; set; }
    public int Recurrence { get; set; }
    public DateTimeOffset FirstDueOn { get; set; }
    public DateTimeOffset? LastDueOn { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
    [MaxLength(256)] public string CreatedByEmail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    [MaxLength(256)] public string? ArchivedByEmail { get; set; }
}

/// <summary>The accountant's word over a due date: the entry behind PlacementKey ("bill:{id}",
/// "receipt:{id}", "manual:{id}:{yyyy-MM-dd}" — WeeklyCashflowMaths owns the vocabulary) is
/// planned for the week starting PlannedWeekStart (Monday, midnight UTC). One row per entry —
/// re-placing updates it, clearing deletes it — with who and when, so the plan reads the same
/// for every colleague.</summary>
public sealed class WeeklyCashflowPlacementEntity
{
    [Key, MaxLength(128)] public string PlacementKey { get; set; } = "";
    public DateTimeOffset PlannedWeekStart { get; set; }
    [MaxLength(256)] public string MovedByEmail { get; set; } = "";
    public DateTimeOffset MovedAt { get; set; }
}

/// <summary>A supplier group — the Xero supplier (contact) names the Supplier bills band pulls
/// together into one line (e.g. "Materials": Grant &amp; Stone, HSS Hire, Skip IT). Pure display
/// arrangement — bills keep their own placement keys — so deletion is hard, not archived.
/// ContactNamesJson is a JSON string array, matched case-insensitively against the aged
/// payables read's contact names.</summary>
public sealed class WeeklyCashflowSupplierGroupEntity
{
    [Key, MaxLength(64)] public string SupplierGroupId { get; set; } = "";
    [MaxLength(200)] public string Name { get; set; } = "";
    [MaxLength(4000)] public string ContactNamesJson { get; set; } = "[]";
    [MaxLength(256)] public string CreatedByEmail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>"Don't count this one": the Xero-fed entry behind PlacementKey (same key vocabulary
/// as placements) is excluded from the plan's arithmetic — typically a bill whose money already
/// goes out through a manual direct-debit item. One row per entry — excluding upserts, restoring
/// deletes — stamped with who and when, so the plan reads the same for every colleague.</summary>
public sealed class WeeklyCashflowExclusionEntity
{
    [Key, MaxLength(128)] public string PlacementKey { get; set; } = "";
    [MaxLength(256)] public string ExcludedByEmail { get; set; } = "";
    public DateTimeOffset ExcludedAt { get; set; }
}
