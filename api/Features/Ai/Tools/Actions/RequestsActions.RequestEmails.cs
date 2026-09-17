using Jewel.JPMS.Api.Features.Requests.Commands;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class RequestsActions
{
    private static IEnumerable<AiAction> RequestEmailActions() => new AiAction[]
    {
        new AiAction(
            Name: "send_request_email",
            Area: "Requests & RFIs",
            Description: "SENDS EMAIL: sends the request's official document PDF from the projects "
                + "mailbox to the resolved client/architect preference (or one ad-hoc "
                + "recipientOverride). saveAsDraftOnly true stops after staging, leaving the reviewed "
                + "draft in Drafts for Outlook instead of sending.",
            CommandType: typeof(SendRequestEmail),
            ResultType: typeof(RequestEmailOutcome),
            AuthorisationType: typeof(SendRequestEmailAuthorisation),
            ValidationType: typeof(SendRequestEmailValidation),
            VisibleTo: RoleSet.Of(
                JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.Architect),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "This reaches the client or architect the moment it succeeds — ALWAYS confirm the "
                + "request and its recipients with the user before calling. The request must be an "
                + "emailable kind (RFI/NOD/EOT) — promote it first if it is still General. requestId "
                + "via find_by_reference. The result's draftMessageId is the handle for "
                + "delete_mailbox_draft if a staged draft has to be withdrawn."),

        new AiAction(
            Name: "send_request_emails",
            Area: "Requests & RFIs",
            Description: "SENDS EMAIL: sends one email per request id given, each carrying that "
                + "request's official document PDF, from the projects mailbox. saveAsDraftOnly true "
                + "leaves them all in Drafts for Outlook instead. A request that cannot be emailed "
                + "(no resolvable recipient, unknown id) is reported in the batch result without "
                + "stopping the others.",
            CommandType: typeof(SendRequestEmails),
            ResultType: typeof(RequestEmailBatch),
            AuthorisationType: typeof(SendRequestEmailsAuthorisation),
            ValidationType: typeof(SendRequestEmailsValidation),
            VisibleTo: RoleSet.Of(
                JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.Architect),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Every one of these reaches a client or architect the moment it succeeds — ALWAYS "
                + "confirm the list with the user before calling. requestIds via find_by_reference or "
                + "list_requests."),

        new AiAction(
            Name: "send_request_reply",
            Area: "Requests & RFIs",
            Description: "SENDS EMAIL: replies in the original email conversation thread to an email "
                + "linked to the request, carrying the request's official document PDF, from the "
                + "projects mailbox. saveAsDraftOnly true leaves the reply in Drafts for Outlook "
                + "instead of sending it.",
            CommandType: typeof(SendRequestReply),
            ResultType: typeof(RequestEmailOutcome),
            AuthorisationType: typeof(SendRequestReplyAuthorisation),
            ValidationType: typeof(SendRequestReplyValidation),
            VisibleTo: RoleSet.Of(
                JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.Architect),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "This reaches the thread's correspondents the moment it succeeds — ALWAYS confirm "
                + "with the user before calling. mailboxMessageId is the Graph id of the conversation "
                + "email to reply to — list_request_correspondence / read_record_emails surface it. "
                + "requestId via find_by_reference. The result's draftMessageId is the handle for "
                + "delete_mailbox_draft if a staged draft has to be withdrawn."),

        new AiAction(
            Name: "resend_request_document",
            Area: "Requests & RFIs",
            Description: "Queues the request's official document PDF for the background worker, which "
                + "STAGES IT AS A DRAFT in the projects mailbox for a person to send from Outlook — "
                + "it does not send, despite the name. Prefer send_request_email, which sends from "
                + "the portal and reports what happened.",
            CommandType: typeof(ResendRequestDocument),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(ResendRequestDocumentAuthorisation),
            ValidationType: typeof(ResendRequestDocumentValidation),
            VisibleTo: RoleSet.Of(
                JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.Architect),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "The work happens in the background, so this returns before the draft exists and "
                + "reports nothing about it — there is no outcome to read back. Only RFI, NOD and EOT "
                + "documents are emailable; promote a General request first. requestId via "
                + "find_by_reference."),
    };
}
