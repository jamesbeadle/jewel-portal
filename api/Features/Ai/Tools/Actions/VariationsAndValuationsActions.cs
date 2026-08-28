using Jewel.JPMS.Api.Features.Boq.Commands;
using Jewel.JPMS.Api.Features.Lads;
using Jewel.JPMS.Api.Features.Lads.Commands;
using Jewel.JPMS.Api.Features.Retention.Commands;
using Jewel.JPMS.Api.Features.ValuationInvoices;
using Jewel.JPMS.Api.Features.ValuationInvoices.Commands;
using Jewel.JPMS.Api.Features.Variations;
using Jewel.JPMS.Api.Features.Variations.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>Variation, valuation-invoice, BoQ, retention and LADs commands as connector actions.
/// Mirrors Features/Variations, Features/ValuationInvoices, Features/Boq, Features/Retention and
/// Features/Lads — each entry's VisibleTo copies its Authorisation class's role set, and the
/// stamps copy exactly what the endpoint stamps server-side.</summary>
internal sealed class VariationsAndValuationsActions : IAiActionSource
{
    // The BoQ authorisations keep their role sets as private fields; these replicate them
    // role-for-role (AddBoqLineAuthorisation / UpdateBoqLineAuthorisation /
    // RemoveBoqLineAuthorisation, and SignOffBoqForProjectAuthorisation).
    private static readonly RoleSet BoqEditors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);
    private static readonly RoleSet BoqSignOffDirectors = RoleSet.Of(JpmsRoles.Director);

    // Replicates the private field shared in spirit by SetProjectRetentionAuthorisation and
    // ConfirmRetentionReleaseAuthorisation — directors and the finance director.
    private static readonly RoleSet RetentionDirectors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector);

    public IEnumerable<AiAction> Build() => new[]
    {
        // ── Variations ────────────────────────────────────────────────────────────────────────

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

        new AiAction(
            Name: "create_valuation_invoice",
            Area: "Valuation invoices",
            Description: "RAISES a monthly valuation invoice against a project — a real financial "
                + "record, created in the Raised state (optionally drawn from a valuation claim). "
                + "With isManual it records a backdated historical invoice directly as Issued or "
                + "Paid, counting fully toward Certified to date and Total Paid.",
            CommandType: typeof(CreateValuationInvoice),
            ResultType: typeof(ValuationInvoice),
            AuthorisationType: typeof(CreateValuationInvoiceAuthorisation),
            ValidationType: typeof(CreateValuationInvoiceValidation),
            VisibleTo: ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm amount and period with the user before calling. projectId comes from "
                + "list_projects. amountPaid/issuedAt/paidAt apply to manual invoices only. The "
                + "ladder for normal invoices is Raised → Submitted → Approved → Issued → Paid."),

        new AiAction(
            Name: "update_valuation_invoice",
            Area: "Valuation invoices",
            Description: "AMENDS a valuation invoice's period and amount — a real financial change. "
                + "Amending a Rejected invoice returns it to Raised, ready to resubmit. For manual "
                + "invoices it may also revise the paid amount and backdated dates, recomputing "
                + "certified/paid totals in the same operation.",
            CommandType: typeof(UpdateValuationInvoice),
            ResultType: typeof(ValuationInvoice),
            AuthorisationType: typeof(ValuationInvoiceWorkflowAuthorisation),
            ValidationType: typeof(UpdateValuationInvoiceValidation),
            VisibleTo: ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Allowed while Raised or Rejected — and at any status for a manual invoice. "
                + "Confirm the new figures with the user before calling."),

        new AiAction(
            Name: "submit_valuation_invoice",
            Area: "Valuation invoices",
            Description: "SUBMITS a Raised valuation invoice to the architect/client for approval "
                + "(Raised → Submitted) — a real state action that freezes a full valuation-report "
                + "snapshot and locks amount and period until the invoice is approved, rejected or "
                + "amended. It changes portal state; it does not itself send an email.",
            CommandType: typeof(SubmitValuationInvoice),
            ResultType: typeof(ValuationInvoice),
            AuthorisationType: typeof(ValuationInvoiceWorkflowAuthorisation),
            ValidationType: null,
            VisibleTo: ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling — the frozen snapshot is what the client "
                + "will be answering to. The invoice must be Raised."),

        new AiAction(
            Name: "approve_valuation_invoice",
            Area: "Valuation invoices",
            Description: "RECORDS the client's approval of a Submitted valuation invoice "
                + "(Submitted → Approved) — a real financial/state action; only record it when the "
                + "client's approval actually exists. The amount still does not count toward "
                + "Certified to date until the invoice is issued.",
            CommandType: typeof(ApproveValuationInvoice),
            ResultType: typeof(ValuationInvoice),
            AuthorisationType: typeof(ValuationInvoiceWorkflowAuthorisation),
            ValidationType: null,
            VisibleTo: ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling. The optional note captures the "
                + "architect's certificate reference/date. The invoice must be Submitted."),

        new AiAction(
            Name: "reject_valuation_invoice",
            Area: "Valuation invoices",
            Description: "RECORDS the client's rejection of a Submitted valuation invoice "
                + "(Submitted → Rejected) — a real state action. The required reason drives the "
                + "amendment; a Rejected invoice is unlocked to amend (back to Raised) or cancel.",
            CommandType: typeof(RejectValuationInvoice),
            ResultType: typeof(ValuationInvoice),
            AuthorisationType: typeof(ValuationInvoiceWorkflowAuthorisation),
            ValidationType: typeof(RejectValuationInvoiceValidation),
            VisibleTo: ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling; only record a rejection the client has "
                + "actually made. reason is required."),

        new AiAction(
            Name: "issue_valuation_invoice",
            Area: "Valuation invoices",
            Description: "ISSUES a valuation invoice — marks the client invoice as sent "
                + "(Approved → Issued, or Raised → Issued for projects that skip the approval "
                + "loop). A real financial action: from this point the amount counts toward "
                + "Certified to date. The skip path freezes a report snapshot if none is linked.",
            CommandType: typeof(IssueValuationInvoice),
            ResultType: typeof(ValuationInvoice),
            AuthorisationType: typeof(IssueValuationInvoiceAuthorisation),
            ValidationType: typeof(IssueValuationInvoiceValidation),
            VisibleTo: ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling — certified totals move."),

        new AiAction(
            Name: "record_valuation_invoice_payment",
            Area: "Valuation invoices",
            Description: "RECORDS the client's payment against a valuation invoice (→ Paid) — a "
                + "real financial record that increases the project's paid total by the amount "
                + "received. Money does not move; this records that it has.",
            CommandType: typeof(RecordValuationInvoicePayment),
            ResultType: typeof(ValuationInvoice),
            AuthorisationType: typeof(RecordValuationInvoicePaymentAuthorisation),
            ValidationType: typeof(RecordValuationInvoicePaymentValidation),
            VisibleTo: ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm the amount received with the user before calling."),

        new AiAction(
            Name: "cancel_valuation_invoice",
            Area: "Valuation invoices",
            Description: "WITHDRAWS a Raised or Rejected valuation invoice (→ Cancelled) — kept for "
                + "the audit trail but excluded from every total; its snapshots are flagged "
                + "superseded.",
            CommandType: typeof(CancelValuationInvoice),
            ResultType: typeof(ValuationInvoice),
            AuthorisationType: typeof(ValuationInvoiceWorkflowAuthorisation),
            ValidationType: null,
            VisibleTo: ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling. Use delete_valuation_invoice to remove "
                + "an invoice entirely."),

        new AiAction(
            Name: "delete_valuation_invoice",
            Area: "Valuation invoices",
            Description: "DELETES a valuation invoice permanently — a real financial action: "
                + "removing an Issued/Paid invoice reduces Certified to date (re-freezing any "
                + "Preapproved claim's totals), and deleting a Paid one rolls its receipt out of "
                + "the project's paid total. There is no undo.",
            CommandType: typeof(DeleteValuationInvoice),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteValuationInvoiceAuthorisation),
            ValidationType: null,
            VisibleTo: ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user, naming the exact invoice and period, before calling."),

        // ── BoQ ───────────────────────────────────────────────────────────────────────────────

        new AiAction(
            Name: "add_boq_line",
            Area: "BoQ",
            Description: "Adds a priced line to a project's Bill of Quantities — description, unit, "
                + "quantity, rate, cost code and discipline. The BoQ is the tender-side pricing "
                + "record the sign-off freezes against.",
            CommandType: typeof(AddBoqLine),
            ResultType: typeof(BoqLineItem),
            AuthorisationType: typeof(AddBoqLineAuthorisation),
            ValidationType: typeof(AddBoqLineValidation),
            VisibleTo: BoqEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects; cost codes from list_cost_codes."),

        new AiAction(
            Name: "update_boq_line",
            Area: "BoQ",
            Description: "Updates an existing BoQ line's details — description, unit, quantity, "
                + "rate, cost code and discipline. The whole line is re-stated each call.",
            CommandType: typeof(UpdateBoqLine),
            ResultType: typeof(BoqLineItem),
            AuthorisationType: typeof(UpdateBoqLineAuthorisation),
            ValidationType: typeof(UpdateBoqLineValidation),
            VisibleTo: BoqEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "boqLineItemId comes from the project's BoQ listing."),

        new AiAction(
            Name: "remove_boq_line",
            Area: "BoQ",
            Description: "Removes a line from a project's Bill of Quantities permanently. There is "
                + "no undo.",
            CommandType: typeof(RemoveBoqLine),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(RemoveBoqLineAuthorisation),
            ValidationType: typeof(RemoveBoqLineValidation),
            VisibleTo: BoqEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which line, by description, before calling."),

        new AiAction(
            Name: "sign_off_boq_for_project",
            Area: "BoQ",
            Description: "SIGNS OFF a project's Bill of Quantities — a real commercial action "
                + "freezing the tender total at sign-off as the baseline record. Directors only.",
            CommandType: typeof(SignOffBoqForProject),
            ResultType: typeof(BoqSignOff),
            AuthorisationType: typeof(SignOffBoqForProjectAuthorisation),
            ValidationType: typeof(SignOffBoqForProjectValidation),
            VisibleTo: BoqSignOffDirectors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling. signedOffByEmail names the signer on the "
                + "record and tenderTotalAtSignOff must match the BoQ's current total — the "
                + "endpoint takes both from the caller, so state them explicitly."),

        // ── Retention ─────────────────────────────────────────────────────────────────────────

        new AiAction(
            Name: "set_project_retention",
            Area: "Retention",
            Description: "Sets or updates a project's deposit and retention terms (upsert, one "
                + "record per project) — retention percent, completion release percent, defects "
                + "period, practical completion date and deposit percent. These are contract terms "
                + "that drive every future claim's deductions, so this is a real financial-facing "
                + "change.",
            CommandType: typeof(SetProjectRetention),
            ResultType: typeof(ProjectRetention),
            AuthorisationType: typeof(SetProjectRetentionAuthorisation),
            ValidationType: typeof(SetProjectRetentionValidation),
            VisibleTo: RetentionDirectors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm the terms with the user before calling. Percentages are whole numbers "
                + "(5 means 5%). projectId comes from list_projects."),

        new AiAction(
            Name: "confirm_retention_release",
            Area: "Retention",
            Description: "CONFIRMS that a retention release milestone actually happened — a real "
                + "financial record that client money moved (the schedule only ever forecasts). "
                + "The amount is frozen on the record and the confirmation timestamp is set "
                + "server-side.",
            CommandType: typeof(ConfirmRetentionRelease),
            ResultType: typeof(ProjectRetention),
            AuthorisationType: typeof(ConfirmRetentionReleaseAuthorisation),
            ValidationType: typeof(ConfirmRetentionReleaseValidation),
            VisibleTo: RetentionDirectors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm milestone and amount with the user before calling. The project must "
                + "already have retention terms (set_project_retention)."),

        // ── LADs ──────────────────────────────────────────────────────────────────────────────

        new AiAction(
            Name: "add_lad_claim",
            Area: "LADs",
            Description: "Records a Liquidated Damages claim the client has notified against the "
                + "project — period, days claimed, rate per week and amount, created in the "
                + "Notified state. Recorded as created by the signed-in user.",
            CommandType: typeof(AddLadClaim),
            ResultType: typeof(LadClaim),
            AuthorisationType: typeof(AddLadClaimAuthorisation),
            ValidationType: typeof(AddLadClaimValidation),
            VisibleTo: LadRoles.AllowedToManageLads,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. raisedAt is the date of the client's "
                + "notice; left null it defaults to now."),

        new AiAction(
            Name: "update_lad_claim",
            Area: "LADs",
            Description: "Updates a recorded LADs claim — its commercial details and its status as "
                + "the claim moves through Notified → Disputed / Agreed / Withdrawn / Settled. "
                + "Marking a claim Agreed or Settled is a real commercial position; the whole "
                + "record is re-stated each call.",
            CommandType: typeof(UpdateLadClaim),
            ResultType: typeof(LadClaim),
            AuthorisationType: typeof(UpdateLadClaimAuthorisation),
            ValidationType: typeof(UpdateLadClaimValidation),
            VisibleTo: LadRoles.AllowedToManageLads,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Read the claim first and carry forward every field that should not change — "
                + "the command replaces the record. Confirm status changes with the user."),
    };

    // Skipped: AcceptVariationRequest — dispatches ICommandHandler<AcceptVariationRequest, VariationOrder>,
    //          but the endpoint has no Authorisation class (an inline VariationRoles.AllowedToManageVariations
    //          check) and no Validation class, so the AiAction pattern's required AuthorisationType cannot be
    //          satisfied without inventing a class that does not exist.
    // Skipped: RejectVariationRequestEndpoint — no command dispatch: the endpoint mutates the
    //          SubcontractorVariationRequest row directly through JpmsContext (no ICommandHandler,
    //          no Authorisation/Validation classes).
}
