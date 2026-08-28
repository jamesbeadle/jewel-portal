using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.WeeklyCashflow;

/// <summary>
/// The editable face of a manual item — everything the Add/Edit dialog captures, shared by
/// create and update so the two routes cannot drift apart. Dates are UK-local calendar dates at
/// midnight UTC (the SiteClock rule); Amount is per occurrence, positive.
/// </summary>
public sealed record WeeklyCashflowItemDetails(
    string Name,
    WeeklyCashflowCategory Category,
    decimal Amount,
    WeeklyCashflowRecurrence Recurrence,
    DateTimeOffset FirstDueOn,
    DateTimeOffset? LastDueOn,
    string? Notes);

/// <summary>Adds a manual outgoing to the weekly plan. CreatedByEmail is stamped server-side
/// from the signed-in user.</summary>
public sealed record CreateWeeklyCashflowItem(
    WeeklyCashflowItemDetails Details,
    string CreatedByEmail = "") : ICommand<WeeklyCashflowItem>;

/// <summary>Rewrites an item's editable face. The creator stamp and any placements of its
/// occurrences are untouched.</summary>
public sealed record UpdateWeeklyCashflowItem(
    string WeeklyCashflowItemId,
    WeeklyCashflowItemDetails Details) : ICommand<WeeklyCashflowItem>;

/// <summary>Retires an item from the grid — soft, stamped, reversible only by the database.
/// ArchivedByEmail is stamped server-side from the signed-in user.</summary>
public sealed record ArchiveWeeklyCashflowItem(
    string WeeklyCashflowItemId,
    string ArchivedByEmail = "") : ICommand<WeeklyCashflowItem>;

/// <summary>
/// Plans an entry into a week — the grid's one moving part. PlacementKey is the entry's stable
/// key (see <see cref="WeeklyCashflowPlacement"/>); PlannedWeekStart is the target week's
/// Monday, midnight UTC — or null to CLEAR the placement and let the entry fall back to its
/// natural week. MovedByEmail is stamped server-side from the signed-in user.
/// </summary>
public sealed record PlaceWeeklyCashflowEntry(
    string PlacementKey,
    DateTimeOffset? PlannedWeekStart,
    string MovedByEmail = "") : ICommand<WeeklyCashflowPlacementAnswer>;

/// <summary>
/// The placement command's answer: the placement as stored, or null when it was CLEARED. The
/// envelope exists so the command never answers with a bare null — a null OkObjectResult goes
/// out as 204 No Content, which the client's JSON read chokes on (JPMS-31996D). A cleared
/// placement is a real answer and deserves a real body.
/// </summary>
public sealed record WeeklyCashflowPlacementAnswer(WeeklyCashflowPlacement? Placement);

/// <summary>
/// Creates or renames a supplier group — a set of Xero supplier (contact) names the grid pulls
/// together into ONE line under Supplier bills (e.g. the material suppliers: Grant &amp; Stone,
/// HSS Hire, Skip IT). SupplierGroupId null = create. Grouping changes how rows READ, never
/// what they add up to. SavedByEmail is stamped server-side from the signed-in user.
/// </summary>
public sealed record SaveWeeklyCashflowSupplierGroup(
    string? SupplierGroupId,
    string Name,
    IReadOnlyList<string> ContactNames,
    string SavedByEmail = "") : ICommand<WeeklyCashflowSupplierGroup>;

/// <summary>Dissolves a supplier group — its bills return to one line per supplier. Hard
/// delete: a group is display arrangement, not a record (placements are untouched). Returns
/// the group as it stood, so the client can un-apply it locally.</summary>
public sealed record DeleteWeeklyCashflowSupplierGroup(
    string SupplierGroupId) : ICommand<WeeklyCashflowSupplierGroup>;

/// <summary>
/// Excludes a Xero-fed entry from the plan (Excluded = true), or restores it. For the bill a
/// DD/manual item already covers — e.g. a one-off Xero bill for a payment that is really taken
/// by the monthly direct debit — so the money is never counted twice. The entry keeps a visible
/// row in the band's Excluded fold; nothing silently disappears. ExcludedByEmail is stamped
/// server-side from the signed-in user.
/// </summary>
public sealed record SetWeeklyCashflowExclusion(
    string PlacementKey,
    bool Excluded,
    string ExcludedByEmail = "") : ICommand<WeeklyCashflowExclusionAnswer>;

/// <summary>The exclusion command's answer: the stored exclusion, or null when it was lifted —
/// enveloped for the same 204 reason as <see cref="WeeklyCashflowPlacementAnswer"/>.</summary>
public sealed record WeeklyCashflowExclusionAnswer(WeeklyCashflowExclusion? Exclusion);
