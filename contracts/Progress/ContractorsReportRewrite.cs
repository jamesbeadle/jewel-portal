using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>What a flag on the rewrite is about: a change of meaning, a word with a compliance
/// meaning left as the site wrote it, a progress hedge moved to Look Ahead, an item left out, two
/// people disagreeing about the same work, or a quantity or completion written narrowly.</summary>
public enum ContractorsReportFlagKind { Changed, Compliance, Hedge, Omitted, Conflict, Scope }

/// <summary>One thing the rewrite did or would not do, for the office to clear before the report
/// is built. The flag list is a gate, not a footnote (Jeremy, 21 Sep 2026).</summary>
public sealed record ContractorsReportFlag(ContractorsReportFlagKind Kind, DateOnly? Day, string Text, bool IsCleared = false);

/// <summary>One day of Section 1 in house language: a one-line summary and the bullets that print.</summary>
public sealed record ContractorsReportRewrittenDay(DateOnly Date, string Summary, IReadOnlyList<string> Bullets);

/// <summary>
/// The week's daily logs rewritten for the client's side — Section 1's bullets in house language,
/// a summary a day, the unfinished halves as Look Ahead candidates, and the flag list saying what
/// changed and why and what looks risky. The raw notes stay on the record untouched, so the
/// translation can always be checked against them; the office edits the bullets and clears the
/// flags, and the PDF is refused while a flag is open.
/// </summary>
public sealed record ContractorsReportRewrite(
    DateTimeOffset RequestedAt,
    IReadOnlyList<ContractorsReportRewrittenDay> Days,
    IReadOnlyList<string> LookAheadCandidates,
    IReadOnlyList<ContractorsReportFlag> Flags)
{
    public int OpenFlagCount => Flags.Count(flag => !flag.IsCleared);
    public bool IsCleared => OpenFlagCount == 0;
}

/// <summary>Asks the portal to rewrite the report's selected days in house language and flag what
/// the report must not say. Replaces any earlier rewrite; the raw notes are never changed.</summary>
public sealed record RewriteContractorsReportWeek(string ContractorsReportId) : ICommand<ContractorsReport>;

/// <summary>Drops the rewrite so Section 1 prints the raw notes again.</summary>
public sealed record DiscardContractorsReportRewrite(string ContractorsReportId) : ICommand<ContractorsReport>;
