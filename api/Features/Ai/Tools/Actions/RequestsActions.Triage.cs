using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class RequestsActions
{
    // Unlocked 2026-08-31 (docs/ai/11 §4): gate classes added in MailboxTriageCommandGates.cs,
    // same TriageRoles.AllowedToTriage set the endpoints' shared Gate() checks.
    private static IEnumerable<AiAction> TriageActions() => new AiAction[]
    {
        new AiAction(
            Name: "create_request_from_message",
            Area: "Correspondence",
            Description: "Raises a request (RFI, RFA, NOD, EOT, General…) FROM a mailbox email — "
                + "the Control Centre's create-from-email: the new record is tagged onto the email "
                + "so its thread reads back under the request.",
            CommandType: typeof(CreateRequestFromMessage),
            ResultType: typeof(Request),
            AuthorisationType: typeof(CreateRequestFromMessageAuthorisation),
            ValidationType: null,
            VisibleTo: TriageRoles.AllowedToTriage,
            EmailStamps: new[] { "RaisedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "messageId from list_triage_queue / get_mailbox_message. scope says how far the "
                + "tag spreads (MessageOnly, ThreadBehindAnchor, EntireThread). If the answer says "
                + "the thread is already filed under another pathway, ASK THE USER before re-calling "
                + "with allowCrossPathway true — the cross-filing is a decision, not a default."),

        new AiAction(
            Name: "discard_mailbox_message",
            Area: "Correspondence",
            Description: "Discards an untriaged mailbox email — sets it aside out of the triage "
                + "queue. Restorable with restore_mailbox_message; nothing is deleted.",
            CommandType: typeof(DiscardMessage),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DiscardMessageAuthorisation),
            ValidationType: null,
            VisibleTo: TriageRoles.AllowedToTriage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "restore_mailbox_message",
            Area: "Correspondence",
            Description: "Restores a discarded mailbox email to the triage queue.",
            CommandType: typeof(RestoreMessage),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(RestoreMessageAuthorisation),
            ValidationType: null,
            VisibleTo: TriageRoles.AllowedToTriage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "remove_mailbox_message_tag",
            Area: "Correspondence",
            Description: "Removes one JPMS tag from a mailbox email — un-filing it from that "
                + "record or category. The email itself is untouched.",
            CommandType: typeof(RemoveTagFromMessage),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(RemoveTagFromMessageAuthorisation),
            ValidationType: null,
            VisibleTo: TriageRoles.AllowedToTriage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "tag is the exact string as message listings return it (e.g. JPMS/REQ-0012)."),
    };
}
