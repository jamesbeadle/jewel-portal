using Jewel.JPMS.Api.Features.DocumentControl;
using Jewel.JPMS.Api.Features.DocumentControl.Commands;
using Jewel.JPMS.Api.Features.RecordLinks.Commands;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.Requests.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>Requests/RFIs, Document Control and correspondence-filing commands as connector
/// actions. Mirrors Features/Requests/Commands, Features/DocumentControl/Commands and the
/// record-agnostic Features/RecordLinks filing commands — each entry's VisibleTo
/// copies its Authorisation class's role set (replicated where the set is a private field), and
/// the stamps copy exactly what the endpoint stamps server-side. Follows CalendarActions, the
/// exemplar file.</summary>
internal sealed class RequestsActions : IAiActionSource
{
    public IEnumerable<AiAction> Build() => new[]
    {
        // ---------------------------------------------------------------- Requests & RFIs

        new AiAction(
            Name: "raise_request",
            Area: "Requests & RFIs",
            Description: "Creates a new request (RFI, RFA, RFC, RFQ, RFP, NOD, EOT or General) on a "
                + "project's register immediately. Nothing is emailed — issuing the official document "
                + "is a separate, explicit step. Fails if the reference is already in use on the project.",
            CommandType: typeof(RaiseRequest),
            ResultType: typeof(Request),
            AuthorisationType: typeof(RaiseRequestAuthorisation),
            ValidationType: typeof(RaiseRequestValidation),
            VisibleTo: RoleSet.Of(
                JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager,
                JpmsRoles.Architect, JpmsRoles.Subcontractor),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. raisedByEmail should be the signed-in user's "
                + "portal email unless the user says the request was raised by someone else. Kind: "
                + "Rfi, Rfa, Rfc, NoticeOfDelay, Rfq, Rfp, ExtensionOfTime or General. Leave the "
                + "backfill fields (raisedAt, respondedAt, responseText, respondedByEmail, status) "
                + "null unless logging a historical record."),

        new AiAction(
            Name: "update_request_details",
            Area: "Requests & RFIs",
            Description: "Overwrites a request's register details — reference, title, description, "
                + "status, value, response, notes and dates — in one write. Fields omitted are not "
                + "kept: the command replaces the details wholesale, so read the request first and "
                + "carry forward everything that should not change.",
            CommandType: typeof(UpdateRequestDetails),
            ResultType: typeof(Request),
            AuthorisationType: typeof(UpdateRequestDetailsAuthorisation),
            ValidationType: typeof(UpdateRequestDetailsValidation),
            VisibleTo: RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Architect),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "requestId is the record id — find_by_reference resolves REQ-0123 / RFI-049. Use "
                + "get_request_context first and echo the current values for anything unchanged. "
                + "Editing the reference onto a number already in use on the project is rejected."),

        new AiAction(
            Name: "update_request_form",
            Area: "Requests & RFIs",
            Description: "Saves the structured body of the request's official document — the itemised "
                + "queries plus the basis-of-queries, response-action-required and impact-if-late "
                + "narrative sections. Replaces the form's content in one write.",
            CommandType: typeof(UpdateRequestForm),
            ResultType: typeof(Request),
            AuthorisationType: typeof(UpdateRequestFormAuthorisation),
            ValidationType: typeof(UpdateRequestFormValidation),
            VisibleTo: RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Architect),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "requestId via find_by_reference. The items list replaces the existing items — "
                + "read the request first and carry forward every item that should stay."),

        new AiAction(
            Name: "promote_request_to_rfi",
            Area: "Requests & RFIs",
            Description: "Promotes a General request to an official RFI: mints the project's next RFI "
                + "reference, re-opens it if it was closed, and unlocks the official document. Nothing "
                + "is emailed or drafted — promotion is a pure register action; preparing the email "
                + "draft is a separate, explicit step.",
            CommandType: typeof(PromoteRequestToRfi),
            ResultType: typeof(Request),
            AuthorisationType: typeof(PromoteRequestToRfiAuthorisation),
            ValidationType: typeof(PromoteRequestToRfiValidation),
            VisibleTo: RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "The minted RFI reference cannot be handed back — the request stays an RFI. "
                + "Confirm with the user before calling. requestId via find_by_reference."),

        new AiAction(
            Name: "enable_rfq_on_request",
            Area: "Requests & RFIs",
            Description: "Marks an RFI as also carrying a Request for Quotation, which unlocks creating "
                + "a Variation Order Quote (VOQ) from it. Only valid on a request that is already an "
                + "RFI. No email is sent.",
            CommandType: typeof(EnableRfqOnRequest),
            ResultType: typeof(Request),
            AuthorisationType: typeof(EnableRfqOnRequestAuthorisation),
            ValidationType: typeof(EnableRfqOnRequestValidation),
            VisibleTo: RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "requestId via find_by_reference (RFI-049)."),

        new AiAction(
            Name: "link_request_to_party",
            Area: "Requests & RFIs",
            Description: "Links a request to the external party it is corresponded with — a client or "
                + "an architect (optionally on behalf of a named client). Passing a null/empty partyId "
                + "unlinks the current party. Changes who the request's outbound documents resolve to.",
            CommandType: typeof(LinkRequestToParty),
            ResultType: typeof(Request),
            AuthorisationType: typeof(LinkRequestToPartyAuthorisation),
            ValidationType: typeof(LinkRequestToPartyValidation),
            VisibleTo: RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "partyKind: Client or Architect. onBehalfOfClientId only applies when the party is "
                + "an architect. requestId via find_by_reference."),

        new AiAction(
            Name: "merge_requests",
            Area: "Requests & RFIs",
            Description: "Merges one General request into another: the merged request's conversation, "
                + "itemised queries, description and tagged emails all move to the survivor, and the "
                + "merged request is closed permanently with an audit stamp. There is no unmerge.",
            CommandType: typeof(MergeRequests),
            ResultType: typeof(Request),
            AuthorisationType: typeof(MergeRequestsAuthorisation),
            ValidationType: typeof(MergeRequestsValidation),
            VisibleTo: RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Both requests must be General (not yet promoted) and on the same project. "
                + "survivorRequestId keeps its reference and title. Confirm with the user which "
                + "request survives before calling — the merge cannot be undone. Ids via "
                + "find_by_reference."),

        new AiAction(
            Name: "close_request",
            Area: "Requests & RFIs",
            Description: "Closes a request as at the chosen date — it drops off the open register "
                + "immediately. Recorded as closed by the signed-in user. No email is sent.",
            CommandType: typeof(CloseRequest),
            ResultType: typeof(RequestCloseOutcome),
            AuthorisationType: typeof(CloseRequestAuthorisation),
            ValidationType: typeof(CloseRequestValidation),
            VisibleTo: RoleSet.Of(
                JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.SiteManager),
            EmailStamps: new[] { "ClosedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "closedAt must be today or earlier; omit it to close as at now. Confirm with the "
                + "user before calling. requestId via find_by_reference."),

        new AiAction(
            Name: "return_request_to_triage",
            Area: "Requests & RFIs",
            Description: "Undoes a triage decision: clears the request's tags from its emails so they "
                + "re-enter the mailbox triage Inbox queue. The request itself and its conversation "
                + "history are kept untouched — only the email context goes back to triage.",
            CommandType: typeof(ReturnRequestToTriage),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(ReturnRequestToTriageAuthorisation),
            ValidationType: typeof(ReturnRequestToTriageValidation),
            VisibleTo: TriageRoles.AllowedToTriage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling — the emails must then be re-triaged by "
                + "hand. requestId via find_by_reference."),

        new AiAction(
            Name: "delete_request",
            Area: "Requests & RFIs",
            Description: "Deletes a request permanently, including its whole conversation history and "
                + "the official document's itemised queries. There is no undo. Administrator only.",
            CommandType: typeof(DeleteRequest),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteRequestAuthorisation),
            ValidationType: typeof(DeleteRequestValidation),
            VisibleTo: RoleSet.Of(Role.Admin),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Irreversible. Confirm with the user, naming the request's reference and title, "
                + "before calling. requestId via find_by_reference."),

        new AiAction(
            Name: "prepare_request_email_draft",
            Area: "Requests & RFIs",
            Description: "Stages an Outlook draft in the projects mailbox carrying the request's "
                + "official document PDF, addressed to the resolved client/architect preference (or "
                + "one ad-hoc recipientOverride). Nothing is sent — the draft waits in the mailbox's "
                + "Drafts folder for a person to review and send.",
            CommandType: typeof(PrepareRequestEmailDraft),
            ResultType: typeof(RequestEmailDraft),
            AuthorisationType: typeof(PrepareRequestEmailDraftAuthorisation),
            ValidationType: typeof(PrepareRequestEmailDraftValidation),
            VisibleTo: RoleSet.Of(
                JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.Architect),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "The request must be an emailable kind (RFI/NOD/EOT) — promote it first if it is "
                + "still General. requestId via find_by_reference. The result's draftMessageId is "
                + "the handle for delete_mailbox_draft if the draft has to be withdrawn."),

        new AiAction(
            Name: "prepare_request_email_drafts",
            Area: "Requests & RFIs",
            Description: "Stages one Outlook draft in the projects mailbox per request id given, each "
                + "carrying that request's official document PDF. Nothing is sent — every draft waits "
                + "in the Drafts folder. A request that cannot be drafted (no resolvable recipient, "
                + "unknown id) is reported in the batch result without stopping the others.",
            CommandType: typeof(PrepareRequestEmailDrafts),
            ResultType: typeof(RequestEmailDraftBatch),
            AuthorisationType: typeof(PrepareRequestEmailDraftsAuthorisation),
            ValidationType: typeof(PrepareRequestEmailDraftsValidation),
            VisibleTo: RoleSet.Of(
                JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.Architect),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "requestIds via find_by_reference or list_requests."),

        new AiAction(
            Name: "prepare_request_reply_draft",
            Area: "Requests & RFIs",
            Description: "Stages an Outlook draft REPLY, in the original email conversation thread, to "
                + "an email linked to the request — carrying the request's official document PDF. "
                + "Nothing is sent — the draft waits in the mailbox's Drafts folder.",
            CommandType: typeof(PrepareRequestReplyDraft),
            ResultType: typeof(RequestEmailDraft),
            AuthorisationType: typeof(PrepareRequestReplyDraftAuthorisation),
            ValidationType: typeof(PrepareRequestReplyDraftValidation),
            VisibleTo: RoleSet.Of(
                JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.Architect),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "mailboxMessageId is the Graph id of the conversation email to reply to — "
                + "list_request_correspondence / read_record_emails surface it. requestId via "
                + "find_by_reference. The result's draftMessageId is the handle for "
                + "delete_mailbox_draft if the draft has to be withdrawn."),

        new AiAction(
            Name: "resend_request_document",
            Area: "Requests & RFIs",
            Description: "SENDS EMAIL: schedules the request's official document PDF to be emailed to "
                + "the project's resolved external recipients (client / architect preference), or to "
                + "one ad-hoc recipientOverride. The send happens in the background shortly after the "
                + "call — there is no draft step and no recall.",
            CommandType: typeof(ResendRequestDocument),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(ResendRequestDocumentAuthorisation),
            ValidationType: typeof(ResendRequestDocumentValidation),
            VisibleTo: RoleSet.Of(
                JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.Architect),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Always confirm with the user before calling — this emails an external party. Only "
                + "RFI, NOD and EOT documents are emailable; promote a General request first. "
                + "requestId via find_by_reference."),

        // ---------------------------------------------------------------- Document control

        new AiAction(
            Name: "send_attachments_to_document_control",
            Area: "Document control",
            Description: "Copies the chosen attachments of one mailbox email into the Document Control "
                + "queue as pending items (a point-in-time copy — the email itself is not consumed or "
                + "moved). Attachments already sent from that message are skipped, so a re-run cannot "
                + "double-send.",
            CommandType: typeof(SendAttachmentsToDocumentControl),
            ResultType: typeof(IReadOnlyList<DocumentControlItem>),
            AuthorisationType: typeof(SendAttachmentsToDocumentControlAuthorisation),
            ValidationType: typeof(SendAttachmentsToDocumentControlValidation),
            VisibleTo: DocumentControlRoles.AllowedToManage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "messageId is a mailbox Graph message id (read_record_emails surfaces them), not a "
                + "request id. projectIdHint is only a filing hint and can be overridden when filing."),

        new AiAction(
            Name: "file_document_as_drawing",
            Area: "Document control",
            Description: "Files a pending Document Control item into a project's drawing register as a "
                + "drawing revision — the item leaves the pending queue and the file becomes a "
                + "versioned drawing the project team sees. No email is sent.",
            CommandType: typeof(FileDocumentAsDrawing),
            ResultType: typeof(DocumentControlItem),
            AuthorisationType: typeof(FileDocumentAsDrawingAuthorisation),
            ValidationType: typeof(FileDocumentAsDrawingValidation),
            VisibleTo: DocumentControlRoles.AllowedToManage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "documentControlItemId comes from the Document Control queue. Pass drawingId to "
                + "add a revision to an existing drawing, or leave it null to create a new one with "
                + "the given drawingCode/title."),

        new AiAction(
            Name: "file_document_as_payment_certificate",
            Area: "Document control",
            Description: "Files a pending Document Control item onto a project's payment certificate "
                + "register, with certificate number, certified amount and issued date — optionally "
                + "linked to a valuation claim. The item leaves the pending queue. No email is sent.",
            CommandType: typeof(FileDocumentAsPaymentCertificate),
            ResultType: typeof(DocumentControlItem),
            AuthorisationType: typeof(FileDocumentAsPaymentCertificateAuthorisation),
            ValidationType: typeof(FileDocumentAsPaymentCertificateValidation),
            VisibleTo: DocumentControlRoles.AllowedToManage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "documentControlItemId comes from the Document Control queue; projectId from "
                + "list_projects. This is a financial record — confirm the certificate number and "
                + "amount with the user before calling."),

        new AiAction(
            Name: "file_document_to_subcontractor",
            Area: "Document control",
            Description: "Files a pending Document Control item onto a subcontractor's record as a "
                + "versioned compliance document (insurance, certification…), superseding the previous "
                + "version of the same kind exactly as a portal upload would. The item leaves the "
                + "pending queue. No email is sent.",
            CommandType: typeof(FileDocumentToSubcontractor),
            ResultType: typeof(DocumentControlItem),
            AuthorisationType: typeof(FileDocumentToSubcontractorAuthorisation),
            ValidationType: typeof(FileDocumentToSubcontractorValidation),
            VisibleTo: DocumentControlRoles.AllowedToManage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "documentControlItemId comes from the Document Control queue. kind is the "
                + "compliance document kind as the portal names it; expiresAt sets the new version's "
                + "expiry where the kind carries one."),

        // ---------------------------------------------------------------- Correspondence filing

        new AiAction(
            Name: "file_unfiled_replies",
            Area: "Correspondence",
            Description: "Files every newer thread reply a record's page reports as not yet filed "
                + "to it — the amber \"newer replies on this thread aren't filed yet\" banner's "
                + "\"File them all here\", performed server-side. Each reply is tagged to the "
                + "record exactly as the button would (message-only: untagged thread siblings keep "
                + "queueing in the Control Centre for their own decisions), it appears in the "
                + "record's Communications list immediately, and the per-reply outcomes say what "
                + "filed and what refused.",
            CommandType: typeof(FileUnfiledReplies),
            ResultType: typeof(FileUnfiledRepliesResult),
            AuthorisationType: typeof(FileUnfiledRepliesAuthorisation),
            ValidationType: typeof(FileUnfiledRepliesValidation),
            VisibleTo: TriageRoles.AllowedToTriage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "type is the record type (Todo, Request, Variation, WorkOrder, Defect, "
                + "BidPackageInvite, …); recordId via find_by_reference — \"TODO-0083\" resolves "
                + "to it. read_record_emails lists any unfiled replies under unfiledReplies, so "
                + "read first, tell the user what would be filed, then call. found 0 means the "
                + "record's list is already complete. A refused reply's error says why (e.g. a "
                + "cross-pathway conflict) — the rest still file; relay refusals rather than "
                + "retrying."),

        new AiAction(
            Name: "file_email_to_record",
            Area: "Correspondence",
            Description: "Files ONE mailbox email to a record by tagging it JPMS/<reference> — the "
                + "same act as tagging it in the Control Centre or \"Find & tag emails\" on a "
                + "record page. The tag IS the association (no copy is stored) and the record's "
                + "Communications list shows the email immediately. The default scope also tags "
                + "the thread behind the email; newer replies still queue for their own decisions.",
            CommandType: typeof(LinkMessageToRecord),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(LinkMessageToRecordAuthorisation),
            ValidationType: typeof(LinkMessageToRecordValidation),
            VisibleTo: TriageRoles.AllowedToTriage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "messageId is the mailbox message id as read_record_emails / read_selected_email "
                + "return it (internetMessageId is an optional stable fallback). type + recordId "
                + "name the record (recordId via find_by_reference). scope is ThreadBehindAnchor "
                + "(default), MessageOnly or EntireThread. If the answer says the thread is "
                + "already filed under another pathway, ASK THE USER before re-calling with "
                + "allowCrossPathway true — never confirm a cross-filing on your own. pathway "
                + "(Client/Subcontractor/Supplier/Internal) matters only for pathway-neutral "
                + "record types like CostCentre. For catching a record up on its own threads, "
                + "prefer file_unfiled_replies."),

        new AiAction(
            Name: "delete_mailbox_draft",
            Area: "Correspondence",
            Description: "Deletes ONE unsent draft from the shared projects mailbox's Drafts folder "
                + "— the undo for the prepare_*_draft actions when a staged draft was superseded or "
                + "raised in error. The mailbox verifies the message really is an unsent draft "
                + "before deleting, so sent or received mail can never be removed this way. Graph "
                + "moves the deleted draft to the mailbox's Deleted Items, where a person can still "
                + "recover it for a while.",
            CommandType: typeof(DeleteMailboxDraft),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteMailboxDraftAuthorisation),
            ValidationType: typeof(DeleteMailboxDraftValidation),
            VisibleTo: RoleSet.Of(
                JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.Architect),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "messageId is the draft's mailbox message id — the prepare_*_draft results "
                + "return it as draftMessageId, and the audit trail's Draft created rows carry it. "
                + "Confirm with the user WHICH draft, by subject, before calling — someone may have "
                + "edited the draft in Outlook since it was staged, and deleting throws their edits "
                + "away too. Only drafts can be deleted; a draft already sent is refused."),
    };

    // Skipped: PostRequestMessage — already dispatched by AiWriteTools.post_request_message; do not duplicate.
    // Skipped: DiscardMessage (MailboxTriageEndpoints) — no Authorisation class: the endpoint gates inline on TriageRoles, and AiAction requires a DI-resolvable authorisation class with Allows.
    // Skipped: RestoreMessage (MailboxTriageEndpoints) — same reason: inline TriageRoles gate, no authorisation class to declare.
    // Skipped: RemoveTagFromMessage (MailboxTriageEndpoints) — same reason: inline TriageRoles gate, no authorisation class to declare.
    // Skipped: AssignMessageToRequest (MailboxTriageEndpoints) — same reason: inline TriageRoles gate, no authorisation class to declare.
    // Skipped: CreateRequestFromMessage (MailboxTriageEndpoints) — same reason: inline TriageRoles gate, no authorisation class (stamps RaisedByEmail inline).
    // Skipped: ReplyInThreadFromMessage (MailboxTriageEndpoints) — same reason: inline TriageRoles gate, no authorisation class (stamps RaisedByEmail inline).
    // Skipped: RetagRequestWorkflowTags — one-off admin sweep with an inline TriageRoles gate; no authorisation or validation classes exist.
    // Skipped: SendMailboxEmail (MailboxIntake/Compose) — multipart/form-data upload shape dispatched to a concrete handler (not an ICommandHandler registration), inline role gate, no authorisation class.
    // (LinkMessageToRecord is no longer skipped — gate classes added 2026-08-28, actions file_email_to_record and file_unfiled_replies above; RecordLinksEndpoints.Gate reads the same TriageRoles.AllowedToTriage set.)
    // Skipped: PrepareProgrammeReplyDraft (RecordLinks) — no Authorisation class: the role set is a private field of the endpoint itself, and there is no validation class either.
    // Skipped: BackfillBucketsEndpoint (RecordLinks) — no command dispatch: the endpoint performs the Graph sweep directly.
    // Skipped: DiscardDocumentControlItem — no Authorisation class: inline DocumentControlRoles gate in DocumentControlItemCommandEndpoints.
    // Skipped: RestoreDocumentControlItem — no Authorisation class: inline DocumentControlRoles gate in DocumentControlItemCommandEndpoints.
    // Skipped: AttachDrawingsToRequest (RequestAttachmentEndpoints) — no Authorisation class: inline private RoleSet gate.
    // Skipped: RemoveRequestAttachment (RequestAttachmentEndpoints) — no Authorisation class: inline private RoleSet gate.
    // Skipped: UploadRequestAttachments (RequestAttachmentEndpoints) — multipart/form-data file upload with no command dispatch.
}
