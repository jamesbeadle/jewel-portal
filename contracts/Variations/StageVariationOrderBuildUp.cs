using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Stages the client-agreed build-up on a variation that is NOT yet approved (Quoting, Issued,
/// Awaiting AI): the priced lines, one per cost centre, and optionally the narrative sections.
/// Nothing reaches the Valuation Report — that is approval's job. The staged total becomes the
/// variation's estimate (the figure the register and the VO document show), and the approve
/// modal opens pre-seeded with these lines, so approval is a check rather than a retype.
///
/// <para>The whole list is stated each time: an empty list clears the staging (the estimate keeps
/// the last figure it was given — clearing the lines is not un-pricing the variation). A null narrative
/// keeps what stands; whitespace clears it (the same rule as UpdateVariationOrderNarratives).
/// Refused on an Approved or Rejected variation. The stager is stamped server-side.</para>
/// </summary>
public sealed record StageVariationOrderBuildUp(
    string VariationOrderId,
    IReadOnlyList<VariationLineInput> Lines,
    string? CommercialBasis = null,
    string? ProgrammeImpact = null,
    string? Exclusions = null,
    string StagedByEmail = "") : ICommand<VariationOrder>;
