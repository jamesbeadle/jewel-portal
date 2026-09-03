using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Xero;

// Keeping Xero's "Cost Code" tracking category in step with the portal's cost-code master
// (2026-09-03, the accountant's "it has not pulled through all the cost codes from the portal to
// Xero": 140 portal codes, 39 in Xero, 101 missing including PRELIMS-SMG and TBC). The portal has
// always created a missing option lazily — the moment a bill line needed it — which is why the
// two drifted without anyone noticing: a code nobody had billed against never reached Xero. This
// is the read that makes the gap visible. DECISION (James, 2026-09-03): the portal does NOT get
// Xero's accounting.settings write scope — tracking options are created and renamed by a person in
// Xero's UI (Settings → Tracking categories), never by the portal. So this is a manual-sync helper:
// it lists exactly what to create, spelt exactly as the coding run will look for it, and the
// approval / coding paths refuse a missing option with that instruction instead of creating it.

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
