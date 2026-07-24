using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Creates a standalone Variation Order directly — in Quoting, with no request/RFQ behind it. The
/// manual-entry route for reconciling historic or client-instructed variations that never ran
/// through the app's RFI → RFQ pipeline (so there is no RFI to pick). RequestId is left empty: the
/// register shows a "No request" badge and the originating RFI can be linked later on the variation
/// itself. Number, when supplied, fixes the VOQ number — and therefore the V-ref minted at approval
/// (VOQ-0050 → V50) — so a manually added variation can be lined up with the reference already shown
/// on a valuation report issued to the client; left null it takes the project's next number. Nothing
/// hits the Valuation Report / CVR / budget here: that write-through still runs through
/// ApproveVariationOrder — this command only creates the draft.
/// </summary>
public sealed record CreateManualVariationOrder(
    string ProjectId,
    string CreatedByEmail,
    string Title,
    string? Description = null,
    decimal? EstimatedValue = null,
    int? Number = null) : ICommand<VariationOrder>;
