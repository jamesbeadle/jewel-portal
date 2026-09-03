using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Xero;

// Keeping Xero's "Cost Code" tracking category in step with the portal's cost-code master
// (2026-09-03, the accountant's "it has not pulled through all the cost codes from the portal to
// Xero": 140 portal codes, 39 in Xero, 101 missing including PRELIMS-SMG and TBC). The portal has
// always created a missing option lazily — the moment a bill line needed it — which is why the
// two drifted without anyone noticing: a code nobody had billed against never reached Xero. This
// is the deliberate version: read the gap, create the missing options in one confirmed batch,
// rename when a spelling must change. Nothing here ever deletes or archives an option in Xero —
// retiring a code is a Xero-UI decision, because an archived option still explains history.

/// <summary>
/// The gap between the portal's cost-code master and Xero's "Cost Code" tracking options, read
/// fresh from Xero. Each active portal code resolves to the option name the coding run would use
/// for it — its current Xero mapping's tracking option when one is set, otherwise the code
/// itself — and that name is what is looked for in Xero.
/// </summary>
public sealed record GetXeroCostCodeOptionGaps : IQuery<XeroCostCodeOptionGaps>;

public sealed record XeroCostCodeOptionGaps(
    bool IsConfigured,
    string? Error,
    // Xero's own name for the category and how many of its options are active / archived — the
    // context a caller needs before creating more (Xero has historically capped active options).
    string? CategoryName,
    int ActiveOptionCount,
    int ArchivedOptionCount,
    // Active portal codes whose option Xero doesn't hold at all — what a create run would add.
    IReadOnlyList<XeroCostCodeOptionGap> Missing,
    // Active portal codes whose option exists in Xero but is ARCHIVED: creating would be refused
    // as a duplicate; the fix is to restore the option in Xero's UI.
    IReadOnlyList<XeroCostCodeOptionGap> Archived,
    // Active portal codes Xero already holds — the healthy majority once the batch has run.
    IReadOnlyList<XeroCostCodeOptionGap> Present,
    // Xero options that match no active portal code (legacy numeric options and the like).
    // Reported, never touched.
    IReadOnlyList<string> XeroOnlyOptions)
{
    public static XeroCostCodeOptionGaps NotConfigured() =>
        new(false, null, null, 0, 0, Array.Empty<XeroCostCodeOptionGap>(), Array.Empty<XeroCostCodeOptionGap>(),
            Array.Empty<XeroCostCodeOptionGap>(), Array.Empty<string>());

    public static XeroCostCodeOptionGaps Failed(string error) =>
        new(true, error, null, 0, 0, Array.Empty<XeroCostCodeOptionGap>(), Array.Empty<XeroCostCodeOptionGap>(),
            Array.Empty<XeroCostCodeOptionGap>(), Array.Empty<string>());
}

/// <summary>One portal code and the Xero option name it codes under.</summary>
public sealed record XeroCostCodeOptionGap(string Code, string Name, string OptionName);

/// <summary>
/// WRITES TO XERO: creates the missing "Cost Code" tracking options for the portal's active cost
/// codes — every one the gap read lists as missing, or only the <paramref name="Codes"/> named.
/// Creates only: an option that already exists (active or archived) is never touched, and
/// nothing is ever deleted or archived. Stops at the first option Xero refuses and reports
/// Xero's message verbatim (the category's option cap is the expected refusal), with what was
/// created before it and what remains. Confirm-first everywhere; every run is audited.
/// </summary>
public sealed record CreateXeroCostCodeOptions(IReadOnlyList<string>? Codes = null)
    : ICommand<XeroCostCodeOptionsCreateResult>;

public sealed record XeroCostCodeOptionsCreateResult(
    bool IsConfigured,
    // Xero's refusal, verbatim, when the run stopped early. Null when every option went in.
    string? Error,
    IReadOnlyList<string> Created,
    // Requested codes that already had an option (active) — nothing to do.
    IReadOnlyList<string> AlreadyPresent,
    // Requested codes whose option is archived in Xero — restore it there; not created.
    IReadOnlyList<string> ArchivedInXero,
    // Requested codes that are not active portal codes — refused rather than guessed.
    IReadOnlyList<string> Unknown,
    // Still missing when the run stopped on Xero's refusal (the one it stopped at first).
    IReadOnlyList<string> NotCreated,
    int ActiveOptionCountAfter);

/// <summary>
/// WRITES TO XERO: renames one existing "Cost Code" tracking option. Xero applies a rename to
/// history — every bill line ever tracked under the old name reads under the new one from then
/// on — so this is confirm-first with that warning shown, and audited. The portal's own
/// cost-code master is NOT changed: when the renamed option was a code's own name, the code's
/// Xero mapping (or the code itself) must be brought in line or the coding run will create the
/// old name again the next time it needs it — the result says so.
/// </summary>
public sealed record RenameXeroCostCodeOption(string CurrentName, string NewName)
    : ICommand<XeroCostCodeOptionRenameResult>;

public sealed record XeroCostCodeOptionRenameResult(
    bool IsConfigured,
    string? Error,
    string? TrackingOptionId,
    string CurrentName,
    string NewName,
    // Plain-English consequences: the history rewrite, and any portal code that still codes
    // under the old name.
    IReadOnlyList<string> Warnings);
