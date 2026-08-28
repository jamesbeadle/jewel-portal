using Jewel.JPMS.Api.Features.Procurement.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>Procurement commands (bid packages, tenders, quotes, work orders) as connector
/// actions. Mirrors Features/Procurement/Commands — each entry's VisibleTo copies its
/// Authorisation class's role set (every procurement authorisation keeps its set private, so the
/// sets are replicated below with the identical roles), and the stamps copy exactly what the
/// endpoint stamps server-side. Follows CalendarActions, THE EXEMPLAR FILE for the pattern.</summary>
internal sealed class ProcurementActions : IAiActionSource
{
    // Mirrors CreateBidPackageAuthorisation / AddBidPackageLineItemsAuthorisation /
    // DeleteBidPackageAuthorisation / SuggestBidPackagesAuthorisation /
    // SetBidPackageLineItemCoverageAuthorisation (all declare this same set privately).
    private static readonly RoleSet PackageCreators = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.ProjectManager,
        JpmsRoles.Estimator, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    // Mirrors the tender-administration gates: CreateBidPackageFromMessageAuthorisation,
    // InviteSubcontractorsToBidPackageAuthorisation, DeclineBidPackageRecipientAuthorisation,
    // RemoveBidPackageRecipientAuthorisation, PrepareBidPackageInviteDraftAuthorisation,
    // PrepareWorkOrderEmailDraftAuthorisation, ExtractTenderFromMessageAuthorisation,
    // RecordTenderResponseAuthorisation, SaveExtractedQuoteAuthorisation,
    // SetBidPackageDrawingsAuthorisation, SetBidPackageLineItemsAuthorisation,
    // UpdateBidPackageScopeAuthorisation, UpdateWorkOrderAuthorisation.
    private static readonly RoleSet PackageAdministrators = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.ProjectManager,
        JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    // Mirrors CloseBidPackageAuthorisation / ReopenBidPackageAuthorisation.
    private static readonly RoleSet PackageClosers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    // Mirrors ReviseQuoteAuthorisation / SubmitQuoteForBidPackageAuthorisation.
    private static readonly RoleSet QuoteWriters = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.ProjectManager,
        JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.Subcontractor);

    // Mirrors CreateManualWorkOrderAuthorisation / CreateWorkOrderFromMessageAuthorisation /
    // ApproveWorkOrderAuthorisation / RejectWorkOrderAuthorisation /
    // DeleteDraftWorkOrderAuthorisation / RecodeWorkOrderLineAuthorisation.
    private static readonly RoleSet WorkOrderRaisers = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // Mirrors CancelWorkOrderAuthorisation — a directors' money decision.
    private static readonly RoleSet WorkOrderCancellers =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector);

    // Mirrors SendWorkOrderPoEmailAuthorisation.
    private static readonly RoleSet PoEmailSenders = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager,
        JpmsRoles.Estimator, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    public IEnumerable<AiAction> Build() => new[]
    {
        // ---- Bid packages -------------------------------------------------------------------

        new AiAction(
            Name: "create_bid_package",
            Area: "Procurement",
            Description: "Creates a new Draft bid package (tender package) on a project — a scope of "
                + "work to put out to subcontractors. Nothing is sent to anyone. Returns the created "
                + "package.",
            CommandType: typeof(CreateBidPackage),
            ResultType: typeof(BidPackage),
            AuthorisationType: typeof(CreateBidPackageAuthorisation),
            ValidationType: typeof(CreateBidPackageValidation),
            VisibleTo: PackageCreators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. ownerEmail is carried in the request over "
                + "HTTP too — pass the signed-in user's email unless the user names another owner. "
                + "Set materialsApplicable true when the invite should ask each subcontractor "
                + "whether they will supply their own materials."),

        new AiAction(
            Name: "create_bid_package_from_message",
            Area: "Procurement",
            Description: "Creates a Draft bid package on a project from a tagged mailbox message and "
                + "links the originating email (and, by default, the thread behind it) to the new "
                + "package via the shared record-link tag. Nothing is sent to anyone. Returns the "
                + "created package.",
            CommandType: typeof(CreateBidPackageFromMessage),
            ResultType: typeof(BidPackage),
            AuthorisationType: typeof(CreateBidPackageFromMessageAuthorisation),
            ValidationType: typeof(CreateBidPackageFromMessageValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: new[] { "OwnerEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "messageId is a mailbox message id from the triage queue, not a request id. "
                + "projectId comes from list_projects. Filing under Subcontractor as well as a "
                + "pathway the thread already carries is refused unless allowCrossPathway is true — "
                + "only pass it after the user confirms."),

        new AiAction(
            Name: "update_bid_package_scope",
            Area: "Procurement",
            Description: "Updates a bid package's header — title, trade, status, owner, materials "
                + "flag and (optionally) specification summary. The whole editable surface travels "
                + "together, so send current values for anything that should not change. Returns the "
                + "updated package.",
            CommandType: typeof(UpdateBidPackageScope),
            ResultType: typeof(BidPackage),
            AuthorisationType: typeof(UpdateBidPackageScopeAuthorisation),
            ValidationType: typeof(UpdateBidPackageScopeValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "bidPackageId comes from list_bid_packages; read the current values first "
                + "(get_bid_package_context) and carry them forward. specificationSummary null "
                + "means leave unchanged."),

        new AiAction(
            Name: "delete_bid_package",
            Area: "Procurement",
            Description: "PERMANENTLY deletes a bid package and everything under it — invite rows, "
                + "line items, quotes and their lines, tender-document attachments and drawing "
                + "links. There is no undo. Tagged emails stay in the mailbox. Refused for an "
                + "Awarded package or while any work order references it.",
            CommandType: typeof(DeleteBidPackage),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteBidPackageAuthorisation),
            ValidationType: typeof(DeleteBidPackageValidation),
            VisibleTo: PackageCreators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Deletion is for packages that should never have existed; close_bid_package is "
                + "the polite no-winner ending for a real tender. Confirm with the user, naming the "
                + "package, before calling. bidPackageId comes from list_bid_packages."),

        new AiAction(
            Name: "close_bid_package",
            Area: "Procurement",
            Description: "Ends a bid package's tender process without picking a winner (all "
                + "tenderers declined, works re-scoped, package lapsed): sets the package Closed and "
                + "stamps ClosedAt. An Awarded package cannot be closed. Returns the updated "
                + "package.",
            CommandType: typeof(CloseBidPackage),
            ResultType: typeof(BidPackage),
            AuthorisationType: typeof(CloseBidPackageAuthorisation),
            ValidationType: typeof(CloseBidPackageValidation),
            VisibleTo: PackageClosers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Reversible via reopen_bid_package. Confirm with the user before calling. "
                + "bidPackageId comes from list_bid_packages."),

        new AiAction(
            Name: "reopen_bid_package",
            Area: "Procurement",
            Description: "Puts a Closed bid package back in play: clears ClosedAt and restores the "
                + "status the package's data implies (QuotesReceived when it holds any tender, "
                + "Inviting when subcontractors were invited, Draft otherwise). Only a Closed "
                + "package can be reopened. Returns the updated package.",
            CommandType: typeof(ReopenBidPackage),
            ResultType: typeof(BidPackage),
            AuthorisationType: typeof(ReopenBidPackageAuthorisation),
            ValidationType: typeof(ReopenBidPackageValidation),
            VisibleTo: PackageClosers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "bidPackageId comes from list_bid_packages."),

        new AiAction(
            Name: "suggest_bid_packages",
            Area: "Procurement",
            Description: "Asks the portal's AI to read the project's live valuation report and "
                + "propose bid packages worth tendering for the remaining works, grouped by trade. "
                + "Nothing is created — the result is a list of proposals the user picks from "
                + "(create_bid_package makes a real one).",
            CommandType: typeof(SuggestBidPackages),
            ResultType: typeof(BidPackageSuggestionResult),
            AuthorisationType: typeof(SuggestBidPackagesAuthorisation),
            ValidationType: typeof(SuggestBidPackagesValidation),
            VisibleTo: PackageCreators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. model is an AI tier key (haiku / sonnet / "
                + "opus / fable); unknown keys degrade to the cheap tier. If the result comes back "
                + "isComplete false, re-send the SAME command with the returned partialText to "
                + "continue the answer."),

        // ---- Package scope: line items, coverage, drawings ----------------------------------

        new AiAction(
            Name: "set_bid_package_line_items",
            Area: "Procurement",
            Description: "REPLACES the full set of scope line items on a bid package with the "
                + "supplied list — existing rows are deleted and recreated with new ids, which drops "
                + "their coverage links and quote-line references. Use add_bid_package_line_items to "
                + "append without touching existing rows. Returns the stored line items.",
            CommandType: typeof(SetBidPackageLineItems),
            ResultType: typeof(IReadOnlyList<BidPackageLineItem>),
            AuthorisationType: typeof(SetBidPackageLineItemsAuthorisation),
            ValidationType: typeof(SetBidPackageLineItemsValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "bidPackageId comes from list_bid_packages. Each line's costCode must be a code "
                + "in the cost-centre master list (list_cost_codes). Because this is a wholesale "
                + "replace, confirm with the user before calling on a package that already has "
                + "lines."),

        new AiAction(
            Name: "add_bid_package_line_items",
            Area: "Procurement",
            Description: "Appends scope line items to a bid package without touching the existing "
                + "set — existing rows keep their ids, coverage links and quote-line references "
                + "exactly as they stand. Returns the package's full stored line-item list.",
            CommandType: typeof(AddBidPackageLineItems),
            ResultType: typeof(IReadOnlyList<BidPackageLineItem>),
            AuthorisationType: typeof(AddBidPackageLineItemsAuthorisation),
            ValidationType: typeof(AddBidPackageLineItemsValidation),
            VisibleTo: PackageCreators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "bidPackageId comes from list_bid_packages. Each line's costCode must be a code "
                + "in the cost-centre master list (list_cost_codes)."),

        new AiAction(
            Name: "set_bid_package_line_item_coverage",
            Area: "Procurement",
            Description: "Links one bid package line item to its commercial home — a cost centre "
                + "(coverage ContractLine + costCode) or a variation order (coverage Variation + "
                + "variationOrderId), never both; coverage Unassigned clears the link. Returns the "
                + "package's full line-item list with the updated coverage.",
            CommandType: typeof(SetBidPackageLineItemCoverage),
            ResultType: typeof(IReadOnlyList<BidPackageLineItem>),
            AuthorisationType: typeof(SetBidPackageLineItemCoverageAuthorisation),
            ValidationType: typeof(SetBidPackageLineItemCoverageValidation),
            VisibleTo: PackageCreators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "lineItemId comes from the package's line items (get_bid_package_context). "
                + "boqLineItemId is legacy only — new contract-side links carry a cost centre."),

        new AiAction(
            Name: "set_bid_package_drawings",
            Area: "Procurement",
            Description: "REPLACES the set of project drawings linked to a bid package (the tender "
                + "documents the invite email attaches) with the supplied list — send the full "
                + "desired set. Returns the linked drawings, newest first.",
            CommandType: typeof(SetBidPackageDrawings),
            ResultType: typeof(IReadOnlyList<Drawing>),
            AuthorisationType: typeof(SetBidPackageDrawingsAuthorisation),
            ValidationType: typeof(SetBidPackageDrawingsValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "bidPackageId comes from list_bid_packages. Wholesale replacement: omitting a "
                + "currently linked drawing unlinks it, so read the current set first."),

        // ---- Tender recipients and invites --------------------------------------------------

        new AiAction(
            Name: "invite_subcontractors_to_bid_package",
            Area: "Procurement",
            Description: "Adds one or more subcontractors to a bid package's tender list and moves "
                + "a Draft package to Inviting. This records the invites in the portal only — no "
                + "email is sent (the invite email is drafted separately with "
                + "prepare_bid_package_invite_draft). Idempotent per subcontractor. Returns the "
                + "package's full recipient list.",
            CommandType: typeof(InviteSubcontractorsToBidPackage),
            ResultType: typeof(IReadOnlyList<BidPackageRecipient>),
            AuthorisationType: typeof(InviteSubcontractorsToBidPackageAuthorisation),
            ValidationType: typeof(InviteSubcontractorsToBidPackageValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "bidPackageId comes from list_bid_packages; subcontractorIds from the "
                + "subcontractor directory."),

        new AiAction(
            Name: "remove_bid_package_recipient",
            Area: "Procurement",
            Description: "Removes one invited subcontractor from a bid package's tender list (the "
                + "invite row, not the directory entry). Returns the package's remaining "
                + "recipients.",
            CommandType: typeof(RemoveBidPackageRecipient),
            ResultType: typeof(IReadOnlyList<BidPackageRecipient>),
            AuthorisationType: typeof(RemoveBidPackageRecipientAuthorisation),
            ValidationType: typeof(RemoveBidPackageRecipientValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Over HTTP both ids are route parameters: bidPackageId from list_bid_packages, "
                + "recipientId from the package's recipient list (get_bid_package_context). Confirm "
                + "with the user before calling."),

        new AiAction(
            Name: "decline_bid_package_recipient",
            Area: "Procurement",
            Description: "Records that an invited subcontractor has declined to tender, or undoes "
                + "that (declined false) when recorded in error — undoing restores Responded when "
                + "they hold a live quote, otherwise Invited. The winning recipient cannot be "
                + "declined. Returns the package's full recipient list.",
            CommandType: typeof(DeclineBidPackageRecipient),
            ResultType: typeof(IReadOnlyList<BidPackageRecipient>),
            AuthorisationType: typeof(DeclineBidPackageRecipientAuthorisation),
            ValidationType: typeof(DeclineBidPackageRecipientValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "recipientId comes from the package's recipient list "
                + "(get_bid_package_context)."),

        new AiAction(
            Name: "prepare_bid_package_invite_draft",
            Area: "Procurement",
            Description: "Creates the tender-invite email as a DRAFT in the shared mailbox — "
                + "NOTHING IS SENT; a person reviews and sends it from Outlook. Every invited "
                + "recipient with a directory email goes in BCC, the package's linked drawings are "
                + "attached, and the draft carries the package's tag so the sent copy and replies "
                + "group under the package.",
            CommandType: typeof(PrepareBidPackageInviteDraft),
            ResultType: typeof(BidPackageInviteDraft),
            AuthorisationType: typeof(PrepareBidPackageInviteDraftAuthorisation),
            ValidationType: typeof(PrepareBidPackageInviteDraftValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "The command drafts exactly the subject and htmlBody it is given — confirm the "
                + "wording with the user first. Invite the subcontractors "
                + "(invite_subcontractors_to_bid_package) before drafting; a package with no "
                + "recipients fails with a readable message."),

        // ---- Tenders and quotes -------------------------------------------------------------

        new AiAction(
            Name: "extract_tender_from_message",
            Area: "Procurement",
            Description: "Reads a subcontractor's tender email (body plus any returned "
                + "pricing-schedule spreadsheet) and proposes the submission with AI: priced lines "
                + "mapped to the package's line items, the subcontractor identified from the "
                + "sender, and every gap named. NOTHING is saved — commit the reviewed proposal "
                + "with save_extracted_quote.",
            CommandType: typeof(ExtractTenderFromMessage),
            ResultType: typeof(TenderExtraction),
            AuthorisationType: typeof(ExtractTenderFromMessageAuthorisation),
            ValidationType: typeof(ExtractTenderFromMessageValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "bidPackageId comes from list_bid_packages; messageId is a mailbox message id "
                + "from the package's correspondence."),

        new AiAction(
            Name: "save_extracted_quote",
            Area: "Procurement",
            Description: "Commits a reviewed tender submission: creates the Quote (value = sum of "
                + "line totals) and its per-line pricing, marks the subcontractor's recipient row "
                + "Responded, and moves an Inviting package to QuotesReceived. Re-submitting for "
                + "the same package and subcontractor REPLACES their previous quote and its lines. "
                + "Returns the Quote.",
            CommandType: typeof(SaveExtractedQuote),
            ResultType: typeof(Quote),
            AuthorisationType: typeof(SaveExtractedQuoteAuthorisation),
            ValidationType: typeof(SaveExtractedQuoteValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Have the user review the lines (e.g. from extract_tender_from_message) before "
                + "committing. Lines align to package line items via bidPackageLineItemId; null "
                + "marks an extra line the subcontractor priced that is not on the package."),

        new AiAction(
            Name: "record_tender_response",
            Area: "Procurement",
            Description: "Marks the bid package recipient matching a sender email as Responded — "
                + "used when an email carrying a subcontractor's tender has been filed to the "
                + "package. Matches by exact directory email, else by a unique company domain; no "
                + "match is a quiet no-op, not a failure. Returns the package's full recipient "
                + "list.",
            CommandType: typeof(RecordTenderResponse),
            ResultType: typeof(IReadOnlyList<BidPackageRecipient>),
            AuthorisationType: typeof(RecordTenderResponseAuthorisation),
            ValidationType: typeof(RecordTenderResponseValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "This never links mail — the filing (tag) is done elsewhere; it only updates the "
                + "recipient's status."),

        new AiAction(
            Name: "submit_quote_for_bid_package",
            Area: "Procurement",
            Description: "Records a headline quote (single value plus notes, no priced lines) from "
                + "a subcontractor on a bid package. Returns the Quote.",
            CommandType: typeof(SubmitQuoteForBidPackage),
            ResultType: typeof(Quote),
            AuthorisationType: typeof(SubmitQuoteForBidPackageAuthorisation),
            ValidationType: typeof(SubmitQuoteForBidPackageValidation),
            VisibleTo: QuoteWriters,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "bidPackageId comes from list_bid_packages; subcontractorId from the package's "
                + "recipient list. save_extracted_quote is the richer path when per-line pricing is "
                + "known."),

        new AiAction(
            Name: "revise_quote",
            Area: "Procurement",
            Description: "Revises an existing quote's value and notes in place. Returns the "
                + "updated Quote.",
            CommandType: typeof(ReviseQuote),
            ResultType: typeof(Quote),
            AuthorisationType: typeof(ReviseQuoteAuthorisation),
            ValidationType: typeof(ReviseQuoteValidation),
            VisibleTo: QuoteWriters,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "quoteId comes from the bid package's quotes (get_bid_package_context)."),

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
                + "wording with the user first. workOrderId comes from list_work_orders."),

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

    // Skipped: UpdateManualWorkOrder — the endpoint overwrites EditorMayEditAnyOrder server-side
    //   from the signed-in user's roles (MD/FD/Admin); the action gateway can only stamp emails and
    //   names, so the flag would appear in the model-facing schema and let any caller grant
    //   themselves the directors-only power to edit awarded/variation/seeded orders.
    // Skipped: SendBidPackageInvite — no Authorisation/Validation classes: the endpoint
    //   (BidPackageInviteComposerEndpoints) checks a private inline RoleSet, and AiAction requires
    //   an authorisation class resolvable from DI. (It also SENDS EMAIL to every invited
    //   subcontractor — deliberately left in the portal.)
    // Skipped: SaveBidPackageInviteComposerDraft — same endpoint, same inline-RoleSet shape: no
    //   Authorisation/Validation classes to declare.
    // Skipped: IssueWorkOrderForVariationOrder — no Authorisation/Validation classes: the endpoint
    //   checks a private inline RoleSet and builds the command from the route + session inline.
    // Skipped: RemoveBidPackageAttachment — no Authorisation/Validation classes: the attachments
    //   endpoint (BidPackageAttachmentEndpoints) checks a private inline RoleSet.
    // Skipped: RemoveWorkOrderAttachment — no Authorisation/Validation classes: the attachments
    //   endpoint (WorkOrderAttachmentEndpoints) checks a private inline RoleSet.
    // Skipped: UploadBidPackageAttachments / UploadWorkOrderAttachments (endpoints) — multipart
    //   file upload (IFormFile), no command dispatch.
    // Skipped: UploadCompanyTenderTerms (endpoint) — multipart file upload of the company terms
    //   PDF, no command dispatch.
}
