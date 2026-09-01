using Jewel.JPMS.Api.Features.Procurement.Commands;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class ProcurementActions
{
    private static IEnumerable<AiAction> WorkOrdersActions() => new AiAction[]
    {
        // ---- Work orders --------------------------------------------------------------------

        new AiAction(
            Name: "create_manual_work_order",
            Area: "Procurement",
            Description: "COMMITS MONEY: raises a work order directly — no bid package, no tender — "
                + "for a subcontractor, with priced lines each carrying its own cost centre and "
                + "amount (the order's value is their sum). Released immediately with the next "
                + "per-project number unless saveAsDraft is true, in which case it is stored as an "
                + "unnumbered Draft until approve_work_order. Does not email the supplier. Returns "
                + "the created work order.",
            CommandType: typeof(CreateManualWorkOrder),
            ResultType: typeof(WorkOrder),
            AuthorisationType: typeof(CreateManualWorkOrderAuthorisation),
            ValidationType: typeof(CreateManualWorkOrderValidation),
            VisibleTo: WorkOrderRaisers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm the lines and value with the user before calling. raisedByEmail should "
                + "be the signed-in user's email (over HTTP it travels in the body, not a server "
                + "stamp). Cost codes come from list_cost_codes. The portal raise dialog's "
                + "uncovered-cost-centre warning gate lives on the HTTP door only and does not run "
                + "here — check the valuation report has a priced sale for each line's centre "
                + "first."),

        new AiAction(
            Name: "create_work_order_from_message",
            Area: "Procurement",
            Description: "COMMITS MONEY: raises a work order from a tagged mailbox message — same "
                + "semantics as create_manual_work_order (priced lines, draft option, numbering) — "
                + "and additionally links the originating email to the new order via the shared "
                + "record-link tag. Ticked email attachments are copied onto the order for record "
                + "keeping (never sent to the supplier). Returns the created work order.",
            CommandType: typeof(CreateWorkOrderFromMessage),
            ResultType: typeof(WorkOrder),
            AuthorisationType: typeof(CreateWorkOrderFromMessageAuthorisation),
            ValidationType: typeof(CreateWorkOrderFromMessageValidation),
            VisibleTo: WorkOrderRaisers,
            EmailStamps: new[] { "RaisedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "messageId is a mailbox message id from the triage queue. Confirm the lines and "
                + "value with the user before calling. Filing under Subcontractor as well as a "
                + "pathway the thread already carries is refused unless allowCrossPathway is true — "
                + "only pass it after the user confirms."),

        new AiAction(
            Name: "approve_work_order",
            Area: "Procurement",
            Description: "Approves a draft work order: mints the next sequential per-project number "
                + "and moves it to Released — the supplier can then see and accept it, and "
                + "allocation, reconciliation and Xero links treat it like any other order. The "
                + "money was already committed as a draft; approval issues the order. Does not "
                + "email the supplier (the portal UI fires send_work_order_po_email separately). "
                + "Returns the updated work order.",
            CommandType: typeof(ApproveWorkOrder),
            ResultType: typeof(WorkOrder),
            AuthorisationType: typeof(ApproveWorkOrderAuthorisation),
            ValidationType: typeof(ApproveWorkOrderValidation),
            VisibleTo: WorkOrderRaisers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling. approvedByEmail should be the signed-in "
                + "user's email (over HTTP it travels in the body, not a server stamp). projectId "
                + "and workOrderId come from list_work_orders."),

        new AiAction(
            Name: "reject_work_order",
            Area: "Procurement",
            Description: "Rejects a draft work order — TERMINAL, there is no un-reject. The draft "
                + "keeps no number and from this point counts nowhere: it drops out of committed "
                + "figures and can never be invoiced, packaged, emailed or accepted. Returns the "
                + "updated work order.",
            CommandType: typeof(RejectWorkOrder),
            ResultType: typeof(WorkOrder),
            AuthorisationType: typeof(RejectWorkOrderAuthorisation),
            ValidationType: typeof(RejectWorkOrderValidation),
            VisibleTo: WorkOrderRaisers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user, naming the order, before calling — raise a fresh order "
                + "instead if it was rejected in error. projectId and workOrderId come from "
                + "list_work_orders."),

        new AiAction(
            Name: "cancel_work_order",
            Area: "Procurement",
            Description: "Cancels (voids) a released work order — TERMINAL, there is no un-cancel. "
                + "The order keeps its minted number and stays on the page as a voided record, but "
                + "its value leaves the issued totals, committed figures, WO allocation and the "
                + "supplier's portal. Refused while anything has been invoiced or paid against it. "
                + "Returns the updated work order.",
            CommandType: typeof(CancelWorkOrder),
            ResultType: typeof(WorkOrder),
            AuthorisationType: typeof(CancelWorkOrderAuthorisation),
            ValidationType: typeof(CancelWorkOrderValidation),
            VisibleTo: WorkOrderCancellers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "A directors' money decision — confirm with the user, naming the order, before "
                + "calling. The supplier is not notified automatically. projectId and workOrderId "
                + "come from list_work_orders."),

        new AiAction(
            Name: "delete_draft_work_order",
            Area: "Procurement",
            Description: "PERMANENTLY deletes a draft work order (undecided or already rejected) — "
                + "its priced lines and attachments (blobs included) go with it, and there is no "
                + "undo. No number was ever minted and nothing went to the supplier, so no gap is "
                + "left. A live order is never deletable — cancel_work_order is the ending for "
                + "those.",
            CommandType: typeof(DeleteDraftWorkOrder),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteDraftWorkOrderAuthorisation),
            ValidationType: typeof(DeleteDraftWorkOrderValidation),
            VisibleTo: WorkOrderRaisers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Deletion is for drafts that should never have existed (raised in error, "
                + "duplicated); reject_work_order records a considered no. Confirm with the user "
                + "before calling. Over HTTP both ids are route parameters — projectId and "
                + "workOrderId come from list_work_orders."),

        new AiAction(
            Name: "update_work_order",
            Area: "Procurement",
            Description: "Updates a work order's headline value and scope text. Returns the "
                + "updated work order.",
            CommandType: typeof(UpdateWorkOrder),
            ResultType: typeof(WorkOrder),
            AuthorisationType: typeof(UpdateWorkOrderAuthorisation),
            ValidationType: typeof(UpdateWorkOrderValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "workOrderId comes from list_work_orders. Changing a value the supplier has "
                + "already been sent is a money change — confirm with the user before calling."),

        new AiAction(
            Name: "recode_work_order_line",
            Area: "Procurement",
            Description: "Re-codes one priced work-order line across cost centres: a single part "
                + "moves the line to another centre; several parts split it by amount (parts must "
                + "total the line exactly). Reshapes where committed value sits without ever "
                + "changing the order's value; paid-to-date follows the split pro-rata. Returns "
                + "the order's full line list.",
            CommandType: typeof(RecodeWorkOrderLine),
            ResultType: typeof(IReadOnlyList<WorkOrderLine>),
            AuthorisationType: typeof(RecodeWorkOrderLineAuthorisation),
            ValidationType: typeof(RecodeWorkOrderLineValidation),
            VisibleTo: WorkOrderRaisers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "workOrderLineId comes from the order's lines (get_work_order_context); cost "
                + "codes from list_cost_codes. Parts are signed like the line's total."),

        new AiAction(
            Name: "send_work_order_po_email",
            Area: "Procurement",
            Description: "SENDS EMAIL: sends the purchase-order email for a released work order to "
                + "the supplier's directory email from the shared projects mailbox, with the given "
                + "subject and HTML body. A failed send leaves the reviewed draft in the mailbox's "
                + "Drafts folder (outcome sent false plus a webLink) and never affects the order. A "
                + "draft or rejected order is refused outright.",
            CommandType: typeof(SendWorkOrderPoEmail),
            ResultType: typeof(WorkOrderPoEmailOutcome),
            AuthorisationType: typeof(SendWorkOrderPoEmailAuthorisation),
            ValidationType: typeof(SendWorkOrderPoEmailValidation),
            VisibleTo: PoEmailSenders,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "The email goes to an external supplier the moment this succeeds — ALWAYS "
                + "confirm the order, recipient, subject and body with the user before calling. "
                + "prepare_work_order_email_draft is the review-in-Outlook alternative. "
                + "workOrderId comes from list_work_orders."),

        new AiAction(
            Name: "prepare_work_order_email_draft",
            Area: "Procurement",
            Description: "Drafts the work-order (purchase-order) email to the supplier in the "
                + "shared mailbox — NOTHING IS SENT; a person reviews and sends it from Outlook. "
                + "The recipient is the supplier's directory email; an order that came from an "
                + "award carries the package's tag so correspondence groups under the package.",
            CommandType: typeof(PrepareWorkOrderEmailDraft),
            ResultType: typeof(WorkOrderEmailDraft),
            AuthorisationType: typeof(PrepareWorkOrderEmailDraftAuthorisation),
            ValidationType: typeof(PrepareWorkOrderEmailDraftValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "The command drafts exactly the subject and htmlBody it is given — confirm the "
                + "wording with the user first. workOrderId comes from list_work_orders. To land "
                + "the purchase order inside an EXISTING email conversation instead, use "
                + "prepare_work_order_reply_draft. The result's draftMessageId is the handle for "
                + "delete_mailbox_draft if the draft has to be withdrawn."),

        new AiAction(
            Name: "prepare_work_order_reply_draft",
            Area: "Procurement",
            Description: "Stages an Outlook draft REPLY, in the original email conversation thread, "
                + "to an email linked to the work order — carrying the rendered purchase-order PDF "
                + "as an attachment. Recipients come from the conversation (reply-all), the draft is "
                + "tagged so the sent copy files under the order, and NOTHING IS SENT — a person "
                + "reviews and sends it from Outlook. Never moves the order's status. A draft, "
                + "rejected or cancelled order is refused outright.",
            CommandType: typeof(PrepareWorkOrderReplyDraft),
            ResultType: typeof(WorkOrderReplyDraft),
            AuthorisationType: typeof(PrepareWorkOrderReplyDraftAuthorisation),
            ValidationType: typeof(PrepareWorkOrderReplyDraftValidation),
            VisibleTo: PackageAdministrators, // mirrors PrepareWorkOrderReplyDraftAuthorisation (same set as the fresh draft)
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "mailboxMessageId is the Graph id of the conversation email to reply to — "
                + "read_record_emails on the work order surfaces it; if the thread isn't linked to "
                + "the order yet, file it first (file_email_to_record) so the reply and the "
                + "conversation live under the order. htmlCoverNote is placed ABOVE the quoted "
                + "history — confirm the wording with the user first; the PO PDF is rendered and "
                + "attached server-side. workOrderId comes from list_work_orders."),

        new AiAction(
            Name: "award_bid_package",
            Area: "Procurement",
            Description: "Awards a bid package to a subcontractor and RAISES A WORK ORDER for the "
                + "awarded value — a real commercial commitment. The compliance guard (insurance and "
                + "certification documents in date) is enforced by validation exactly as in the portal.",
            CommandType: typeof(AwardBidPackage),
            ResultType: typeof(WorkOrder),
            AuthorisationType: typeof(AwardBidPackageAuthorisation),
            ValidationType: typeof(AwardBidPackageValidation),
            VisibleTo: PackageClosers, // mirrors AwardBidPackageAuthorisation.RolesThatMayAwardPackages (Director, PM)
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm subcontractor, value and scope wording with the user before calling — this "
                + "creates the work order immediately. awardedByEmail should be the signed-in user's "
                + "email. bidPackageId from list_bid_packages; quoteId optional (the winning quote)."),
    };
}
