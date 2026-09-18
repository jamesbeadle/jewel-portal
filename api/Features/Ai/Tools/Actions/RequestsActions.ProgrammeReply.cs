using Jewel.JPMS.Api.Features.RecordLinks.Commands;
using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class RequestsActions
{
    private static IEnumerable<AiAction> ProgrammeReplyActions() => new AiAction[]
    {
        new AiAction(
            Name: "send_programme_reply",
            Area: "Correspondence",
            Description: "SENDS EMAIL: replies, in the original email conversation thread, to a "
                + "programme-tagged email on the project — the Programme tab's Communications "
                + "Reply box, performed server-side. Recipients, subject and the quoted history "
                + "come from the conversation (reply-all); the body is written as plain text and "
                + "sanitised server-side. saveAsDraftOnly true stops after staging, leaving the "
                + "reviewed draft in Drafts for Outlook instead of sending. A failed send leaves "
                + "that same draft (outcome sent false plus a webLink).",
            CommandType: typeof(SendProgrammeReply),
            ResultType: typeof(ProgrammeReplyOutcome),
            AuthorisationType: typeof(SendProgrammeReplyAuthorisation),
            ValidationType: typeof(SendProgrammeReplyValidation),
            VisibleTo: RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "The reply goes to whoever is on that conversation the moment this succeeds — "
                + "read the thread first (read_record_emails on the programme, or "
                + "list_mailbox_conversation) and show the user the recipients and the body before "
                + "calling. Answer every question the incoming email asks before asking one of "
                + "your own. messageId is the original email's mailbox message id from those "
                + "reads; saveAsDraftOnly true is the review-in-Outlook alternative."),
    };
}
