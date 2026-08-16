using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Procurement;

/// <summary>
/// Asks Claude to read the project's live valuation report — what is complete, what is left —
/// and propose the bid packages worth tendering for the remaining works, grouped by trade so
/// each package is work one speciality can deliver in one phase without waiting on another
/// package. Nothing is created: the result is a list of proposals the user picks from.
///
/// <para><see cref="Model"/> is an AiModelCatalogue tier KEY (haiku/sonnet/opus/fable) — the
/// same picker as the chat panel. Only the key crosses the wire; the server maps it to a model
/// id, and unknown keys degrade to the cheap tier.</para>
///
/// <para><see cref="PartialText"/> is the continuation mechanism: the full answer is too long
/// for one request under the Static Web Apps gateway's ~45s ceiling on the slower tiers, so
/// each request asks Claude for one bounded chunk. When the result comes back
/// <c>IsComplete == false</c>, the client immediately re-sends the SAME command with the
/// accumulated <see cref="BidPackageSuggestionResult.PartialText"/> and Claude carries on from
/// exactly where it stopped (assistant prefill). Empty = first hop. Stateless on the server,
/// mirroring ContinueAiTurn.</para>
/// </summary>
public sealed record SuggestBidPackages(
    string ProjectId,
    string Model,
    string PartialText = "") : ICommand<BidPackageSuggestionResult>;

/// <summary>One proposed bid package. <see cref="Scope"/> is written to read as the package's
/// "what this covers" summary (it seeds SpecificationSummary if the package is created);
/// <see cref="ApproxValue"/> is the remaining (unclaimed) value of the report lines behind it —
/// an ordering hint, never a tender figure.</summary>
public sealed record BidPackageSuggestion(
    string Title,
    string Trade,
    string Scope,
    decimal ApproxValue,
    bool MaterialsApplicable,
    string Rationale,
    IReadOnlyList<string> SourceLines);

/// <summary>The proposals plus how they were produced. <see cref="ModelUsed"/> is the tier's
/// display name ("Sonnet"); <see cref="Note"/> carries anything the user should know about the
/// basis — e.g. that no claim exists yet so every line was treated as 0% complete, or that the
/// AI is not configured. An empty suggestion list with a Note is a real answer, not an error.
///
/// <para>When <see cref="IsComplete"/> is false Claude stopped mid-answer at the chunk budget:
/// <see cref="Suggestions"/> is empty, <see cref="PartialText"/> holds everything produced so
/// far, and the client re-sends the command with it to continue. Only a complete result carries
/// suggestions.</para></summary>
public sealed record BidPackageSuggestionResult(
    IReadOnlyList<BidPackageSuggestion> Suggestions,
    string ModelUsed,
    string? Note,
    bool IsComplete = true,
    string PartialText = "");
