using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Re-states the narrative sections of a variation order's official document — the commercial
/// basis, the programme impact and the exclusions. Wording only, allowed at EVERY stage for the
/// same reason a retitle is: the number, not the prose, is what the client's paperwork is keyed
/// to, and the document is rendered fresh from the record on every download and send, so a
/// correction reaches the next copy immediately.
///
/// Scope is deliberately the three narrative fields alone: title, value, lines and status each
/// have their own command, so editing the wording can never be the thing that quietly moved a
/// figure. Null clears a section — the document simply omits it.
/// </summary>
public sealed record UpdateVariationOrderNarratives(
    string VariationOrderId,
    string? CommercialBasis,
    string? ProgrammeImpact,
    string? Exclusions) : ICommand<VariationOrder>;
