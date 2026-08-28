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
/// natural week. Returns the placement as stored, or null when it was cleared. MovedByEmail is
/// stamped server-side from the signed-in user.
/// </summary>
public sealed record PlaceWeeklyCashflowEntry(
    string PlacementKey,
    DateTimeOffset? PlannedWeekStart,
    string MovedByEmail = "") : ICommand<WeeklyCashflowPlacement?>;
