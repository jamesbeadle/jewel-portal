using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Creates a standalone Variation Order directly — in ISSUED, with no request/RFQ behind it. The
/// manual-entry route for reconciling historic or client-instructed variations that never ran
/// through the app's RFI → RFQ pipeline (so there is no RFI to pick). A variation raised by hand
/// is one that has already gone out to the client — there is nothing left to quote — so it lands
/// in Issued with IssuedAt stamped, not Quoting (Nigel, 2026-09-14); approval is still the client's
/// instruction, recorded later through ApproveVariationOrder.
///
/// <para>Lines is the priced build-up, captured at creation rather than staged afterwards: one line
/// per cost centre, at least one, and their total is the variation's estimate (the figure the
/// register and the VO document show). The lines are held as the staged build-up exactly as
/// StageVariationOrderBuildUp holds them, so the approve modal opens pre-seeded and approval is a
/// check rather than a retype. Nothing hits the Valuation Report / CVR / budget here: that
/// write-through still runs through ApproveVariationOrder.</para>
///
/// <para>RequestId is left empty: the register shows a "No request" badge and the originating RFI
/// can be linked later on the variation itself. Number, when supplied, fixes the VOQ number — and
/// therefore the V-ref minted at approval (VOQ-0050 → V50) — so a manually added variation can be
/// lined up with the reference already shown on a valuation report issued to the client; left null
/// it takes the project's next number.</para>
/// </summary>
public sealed record CreateManualVariationOrder(
    string ProjectId,
    string CreatedByEmail,
    string Title,
    IReadOnlyList<VariationLineInput> Lines,
    string? Description = null,
    int? Number = null,
    // Narrative sections of the issued VO document — commercial basis, programme impact and
    // exclusions, captured at creation so the document is complete from the first render. All
    // optional, and editable later via UpdateVariationOrderNarratives.
    string? CommercialBasis = null,
    string? ProgrammeImpact = null,
    string? Exclusions = null) : ICommand<VariationOrder>;
