using Jewel.JPMS.Api.Features.Requests.Commands;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class RequestsActions
{
    private static IEnumerable<AiAction> RequestEmailActions() => new AiAction[]
    {
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
    };
}
