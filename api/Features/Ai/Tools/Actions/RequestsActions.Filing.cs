using Jewel.JPMS.Api.Features.RecordLinks.Commands;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.Requests.Commands;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class RequestsActions
{
    private static IEnumerable<AiAction> FilingActions() => new AiAction[]
    {
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
            Notes: "messageId is the mailbox message id as read_record_emails / get_mailbox_message "
                + "return it (internetMessageId is an optional stable fallback). type + recordId "
                + "name the record (recordId via find_by_reference; for a valuation claim — type "
                + "ValuationClaim, the live period's own correspondence — it is the claim's "
                + "ValuationClaimId from get_valuation_context, and for a frozen statement — type "
                + "ValuationReportSnapshot — an id from list_valuation_snapshots). scope is ThreadBehindAnchor "
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
}
