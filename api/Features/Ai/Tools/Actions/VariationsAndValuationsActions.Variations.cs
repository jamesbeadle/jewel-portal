using Jewel.JPMS.Api.Features.Boq.Commands;
using Jewel.JPMS.Api.Features.Lads;
using Jewel.JPMS.Api.Features.Lads.Commands;
using Jewel.JPMS.Api.Features.Retention.Commands;
using Jewel.JPMS.Api.Features.ValuationInvoices;
using Jewel.JPMS.Api.Features.ValuationInvoices.Commands;
using Jewel.JPMS.Api.Features.Variations;
using Jewel.JPMS.Api.Features.Variations.Commands;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class VariationsAndValuationsActions
{
    private static IEnumerable<AiAction> VariationsActions() => new AiAction[]
    {
        new AiAction(
            Name: "create_manual_variation_order",
            Area: "Variations",
            Description: "Creates a standalone variation order in Quoting with no request/RFQ behind "
                + "it — the manual route for historic or client-instructed variations. Nothing hits "
                + "the Valuation Report, CVR or budget until approval. Recorded as created by the "
                + "signed-in user.",
            CommandType: typeof(CreateManualVariationOrder),
            ResultType: typeof(VariationOrder),
            AuthorisationType: typeof(CreateManualVariationOrderAuthorisation),
            ValidationType: typeof(CreateManualVariationOrderValidation),
            VisibleTo: VariationRoles.AllowedToManageVariations,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. number, when supplied, fixes the VOQ number "
                + "(and so the V-ref minted at approval); omit it to take the project's next number."),

        new AiAction(
            Name: "create_voq_from_rfq",
            Area: "Variations",
            Description: "Creates the variation order (VOQ, in Quoting) from a request's RFQ — one "
                + "per request; title/description default to the request's own when omitted. "
                + "Recorded as created by the signed-in user.",
            CommandType: typeof(CreateVoqFromRfq),
            ResultType: typeof(VariationOrder),
            AuthorisationType: typeof(CreateVoqFromRfqAuthorisation),
            ValidationType: typeof(CreateVoqFromRfqValidation),
            VisibleTo: VariationRoles.AllowedToManageVariations,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "requestId is the request's id — find_by_reference resolves REQ-0123 / RFI-049. "
                + "The request must have an RFQ enabled and no existing variation order."),

        new AiAction(
            Name: "approve_variation_order",
            Area: "Variations",
            Description: "APPROVES a variation order — a real financial action recording the "
                + "client's instruction to proceed. In one transaction it mints the V-ref, records "
                + "the agreed value and cost code, writes Variation line(s) into the Valuation "
                + "Report, records a QS accrual on the CVR and commits the value to the cost-centre "
                + "budget(s), then marks the order Approved.",
            CommandType: typeof(ApproveVariationOrder),
            ResultType: typeof(VariationOrder),
            AuthorisationType: typeof(ApproveVariationOrderAuthorisation),
            ValidationType: typeof(ApproveVariationOrderValidation),
            VisibleTo: VariationRoles.AllowedToApproveVariations,
            EmailStamps: new[] { "ApprovedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling — this writes contract figures. Allowed "
                + "from Quoting or Issued. find_by_reference resolves V72 / VOQ references to the "
                + "variationOrderId. When lines are supplied the value is their sum and each cost "
                + "centre is committed its own share; otherwise value (defaulting to the estimate) "
                + "goes against the single costCode."),

        new AiAction(
            Name: "reject_variation_order",
            Area: "Variations",
            Description: "REJECTS a variation order — a real state action recording the client's "
                + "decision not to proceed. If the order was Approved this reverses the approval's "
                + "commercial writes (Valuation Report line, CVR accrual, budget commitment).",
            CommandType: typeof(RejectVariationOrder),
            ResultType: typeof(VariationOrder),
            AuthorisationType: typeof(RejectVariationOrderAuthorisation),
            ValidationType: typeof(RejectVariationOrderValidation),
            VisibleTo: VariationRoles.AllowedToApproveVariations,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling. A rejected order stays on the register as "
                + "a real decision — for repairing an approval made in error use "
                + "return_variation_order_to_quoting instead. find_by_reference resolves V72."),

        new AiAction(
            Name: "return_variation_order_to_quoting",
            Area: "Variations",
            Description: "UN-APPROVES a variation order back to Quoting — internal data repair for "
                + "an approval that should never have happened, reversing whatever the approval "
                + "wrote (valuation line back to TBC, approval accrual deleted, budget released) "
                + "and clearing the V-ref.",
            CommandType: typeof(ReturnVariationOrderToQuoting),
            ResultType: typeof(VariationOrder),
            AuthorisationType: typeof(ReturnVariationOrderToQuotingAuthorisation),
            ValidationType: typeof(ReturnVariationOrderToQuotingValidation),
            VisibleTo: VariationRoles.AllowedToManageVariations,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling. Refused when work orders instruct the "
                + "variation, when its value has been revised, when it is priced as split detail "
                + "lines, or when value has been claimed against it. Not a substitute for "
                + "reject_variation_order — that records a real client decision."),

        new AiAction(
            Name: "delete_variation_order",
            Area: "Variations",
            Description: "DELETES a variation order raised in error, permanently, cascading its "
                + "quoting-stage tender data (bid packages, invited subcontractors, quotes, linked "
                + "drawings) and unlinking any subcontractor variation request that pointed at it. "
                + "There is no undo.",
            CommandType: typeof(DeleteVariationOrder),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteVariationOrderAuthorisation),
            ValidationType: typeof(DeleteVariationOrderValidation),
            VisibleTo: VariationRoles.AllowedToManageVariations,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user, naming the exact variation, before calling. Refused for "
                + "an Approved order (reject it or return it to quoting first) and while any work "
                + "order instructs it. find_by_reference resolves V72."),

        new AiAction(
            Name: "set_variation_order_status",
            Area: "Variations",
            Description: "Moves a variation order between its side-effect-free stages (Quoting, "
                + "Issued, Awaiting AI, and Rejected from an unapproved state) — the status pill's "
                + "dropdown. Entering Issued stamps IssuedAt; moving back to Quoting clears it.",
            CommandType: typeof(SetVariationOrderStatus),
            ResultType: typeof(VariationOrder),
            AuthorisationType: typeof(SetVariationOrderStatusAuthorisation),
            ValidationType: typeof(SetVariationOrderStatusValidation),
            VisibleTo: VariationRoles.AllowedToManageVariations,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "The ladder is Quoting → Issued → Approved. Approving is refused here (it writes "
                + "commercial records — use approve_variation_order), and an Approved order can only "
                + "leave via reject_variation_order or return_variation_order_to_quoting."),

        new AiAction(
            Name: "rename_variation_order",
            Area: "Variations",
            Description: "Retitles a variation order — a wording correction allowed at every stage. "
                + "Nothing already written downstream (valuation report lines, CVR accruals, budget "
                + "commitments) is rewritten; only writes made after the rename carry the new title.",
            CommandType: typeof(RenameVariationOrder),
            ResultType: typeof(VariationOrder),
            AuthorisationType: typeof(RenameVariationOrderAuthorisation),
            ValidationType: typeof(RenameVariationOrderValidation),
            VisibleTo: VariationRoles.AllowedToManageVariations,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Title only — description, value, lines and status each have their own action. "
                + "find_by_reference resolves V72."),

        new AiAction(
            Name: "update_variation_order_narratives",
            Area: "Variations",
            Description: "Re-states the narrative sections of a variation order's official document "
                + "— commercial basis, programme impact and exclusions. Wording only, allowed at "
                + "every stage; the document is rendered fresh on every download so a correction "
                + "reaches the next copy immediately.",
            CommandType: typeof(UpdateVariationOrderNarratives),
            ResultType: typeof(VariationOrder),
            AuthorisationType: typeof(UpdateVariationOrderNarrativesAuthorisation),
            ValidationType: typeof(UpdateVariationOrderNarrativesValidation),
            VisibleTo: VariationRoles.AllowedToManageVariations,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "All three sections are stated each call; null clears a section and the document "
                + "omits it. No figures move here."),

        new AiAction(
            Name: "set_variation_order_estimate",
            Area: "Variations",
            Description: "Re-states a PRE-approval variation order's estimate — the quoting-stage "
                + "figure the register, the VO document and the valuation export's Pending tab "
                + "read. Null or zero marks the order as currently unpriced. No commercial records "
                + "are written.",
            CommandType: typeof(SetVariationOrderEstimate),
            ResultType: typeof(VariationOrder),
            AuthorisationType: typeof(SetVariationOrderEstimateAuthorisation),
            ValidationType: typeof(SetVariationOrderEstimateValidation),
            VisibleTo: VariationRoles.AllowedToManageVariations,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Refused once a build-up is staged (the staged lines' total IS the estimate), on "
                + "Approved orders (use revise_variation_order_value) and on Rejected ones."),

        new AiAction(
            Name: "stage_variation_order_build_up",
            Area: "Variations",
            Description: "Stages the client-agreed priced build-up on a NOT-yet-approved variation "
                + "(Quoting, Issued, Awaiting AI): the lines, one per cost centre, and optionally "
                + "the narratives. Nothing reaches the Valuation Report — the staged total becomes "
                + "the estimate and pre-seeds the approve panel. Recorded as staged by the "
                + "signed-in user.",
            CommandType: typeof(StageVariationOrderBuildUp),
            ResultType: typeof(VariationOrder),
            AuthorisationType: typeof(StageVariationOrderBuildUpAuthorisation),
            ValidationType: typeof(StageVariationOrderBuildUpValidation),
            VisibleTo: VariationRoles.AllowedToManageVariations,
            EmailStamps: new[] { "StagedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "The whole line list is stated each call — an empty list clears the staging "
                + "(the estimate keeps its last figure). A null narrative keeps what stands; "
                + "whitespace clears it. Refused on Approved or Rejected variations."),

        new AiAction(
            Name: "revise_variation_order_value",
            Area: "Variations",
            Description: "REVISES the value of an APPROVED variation order — a real financial "
                + "action: the new value writes through to the Variation line on the Valuation "
                + "Report, the CVR (as a delta QS accrual) and the cost-centre budget commitment. "
                + "Recorded as revised by the signed-in user.",
            CommandType: typeof(ReviseVariationOrderValue),
            ResultType: typeof(VariationOrder),
            AuthorisationType: typeof(ReviseVariationOrderValueAuthorisation),
            ValidationType: typeof(ReviseVariationOrderValueValidation),
            VisibleTo: VariationRoles.AllowedToManageVariations,
            EmailStamps: new[] { "RevisedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling — contract figures move. Refused before "
                + "approval (edit the estimate instead). find_by_reference resolves V72."),

        new AiAction(
            Name: "revise_variation_order_lines",
            Area: "Variations",
            Description: "RE-STATES the priced line build-up of an APPROVED variation order — a "
                + "real financial action: the order's value becomes the lines' sum and the "
                + "commercial records move by the difference (delta QS accrual on the CVR, each "
                + "cost centre's committed budget adjusted). Recorded as revised by the signed-in "
                + "user.",
            CommandType: typeof(ReviseVariationOrderLines),
            ResultType: typeof(VariationOrder),
            AuthorisationType: typeof(ReviseVariationOrderLinesAuthorisation),
            ValidationType: typeof(ReviseVariationOrderLinesValidation),
            VisibleTo: VariationRoles.AllowedToManageVariations,
            EmailStamps: new[] { "RevisedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling. A line carrying a valuationLineItemId is "
                + "re-priced in place (keeping its claim history); one without is added; an "
                + "unclaimed report line missing from the list is dropped. Refused before approval, "
                + "while the latest claim is preapproved, and when a line carrying settled value "
                + "would be dropped — re-price that line to nothing instead."),

        new AiAction(
            Name: "select_voq_tender",
            Area: "Variations",
            Description: "Records the agreed subcontractor (and their agreed value) on a quoting "
                + "variation order — who the works will be instructed to if the variation is "
                + "approved. Quoting-stage data only; the order's status does not change.",
            CommandType: typeof(SelectVoqTender),
            ResultType: typeof(VariationOrder),
            AuthorisationType: typeof(SelectVoqTenderAuthorisation),
            ValidationType: typeof(SelectVoqTenderValidation),
            VisibleTo: VariationRoles.AllowedToManageVariations,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Despite the historic name, no bid package is involved — tenders run and are "
                + "awarded on the bid package separately. find_by_reference resolves V72."),

        new AiAction(
            Name: "link_voq_to_request",
            Area: "Variations",
            Description: "Attaches a variation order to the request (RFI) it belongs to — a repair "
                + "action for records that predate the link, so the register can navigate "
                + "Request → RFI → VO.",
            CommandType: typeof(LinkVoqToRequest),
            ResultType: typeof(VariationOrder),
            AuthorisationType: typeof(LinkVoqToRequestAuthorisation),
            ValidationType: typeof(LinkVoqToRequestValidation),
            VisibleTo: VariationRoles.AllowedToManageVariations,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "The request must belong to the same project, and a request can carry at most "
                + "one variation order. find_by_reference resolves V72 and REQ-0123."),

        // ── Valuation invoices ────────────────────────────────────────────────────────────────

    };
}
