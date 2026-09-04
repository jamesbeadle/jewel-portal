using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Contracts.MailboxCompose;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class RequestsActions
{
    // Unlocked 2026-09-04: the Control Centre's Reply box, Compose pane and Outbox all post this
    // one command, and its plain-JSON shape (no uploaded files) is exactly what a connector call
    // is. The gate class mirrors the endpoint's inline JpmsRoleSets.AllInternal check; the
    // handler's own guards (recipients, subject, body, the client wall) answer as messages, the
    // same courtesy the endpoint's 400s give the composer.
    private static IEnumerable<AiAction> MailboxActions() => new AiAction[]
    {
        new AiAction(
            Name: "send_mailbox_email",
            Area: "Correspondence",
            Description: "SENDS EMAIL from the shared projects mailbox — the Control Centre's "
                + "Reply box and Compose pane, performed server-side. With replyToMessageId it is "
                + "a REPLY inside that email's conversation (forward true makes it a forward "
                + "instead); without one it is a brand-new email. The envelope given is exactly "
                + "what goes on the wire; the projects mailbox is Cc'd automatically. A sent reply "
                + "tags the inbound thread JPMS/Replied (plus the pathway) so it leaves the triage "
                + "queue (markThreadHandled, default true); linkRecordType + linkRecordId file the "
                + "thread to a record in the same act; alsoRaiseRequest (with projectId) raises a "
                + "General request carrying the reply. saveAsDraftOnly true stops after staging, "
                + "leaving the reviewed draft in the mailbox's Drafts folder for a person to send "
                + "from Outlook. A failed send also leaves that draft (sent false plus a webLink) "
                + "and triages nothing. Once sent, an email cannot be recalled — there is no undo.",
            CommandType: typeof(SendMailboxEmail),
            ResultType: typeof(ComposeOutcome),
            AuthorisationType: typeof(SendMailboxEmailAuthorisation),
            ValidationType: null,
            VisibleTo: JpmsRoleSets.AllInternal, // mirrors SendMailboxEmailAuthorisation (every internal role, decision 2026-08-10)
            EmailStamps: new[] { "SenderEmail" },
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Read the email first: get_mailbox_message returns the messageId and a "
                + "ready-made replyAll envelope (to, cc, subject) — the same prefill the Control "
                + "Centre's Reply box shows. replyToMessageId is that messageId; "
                + "replyToInternetMessageId is the listing row's internetMessageId (list_triage_queue, "
                + "search_mailbox), passed when you have it. Only ever use addresses you have "
                + "actually read — never a constructed one. to/cc/bcc are arrays of { email, name } and "
                + "cc, bcc and attachments take [] when there are none. Write the body as PLAIN "
                + "TEXT with blank lines between paragraphs and bodyIsHtml false. pathway "
                + "(Client, Subcontractor, Supplier or Internal) is the side the thread files "
                + "under when the reply triages it; a thread already on a side keeps it. "
                + "Attachments go by reference only: source Drawing (a drawing revision id), "
                + "ProgressPhoto, OriginalMessage (an attachment of the replied-to email, with "
                + "sourceMessageId) or RecordDocument (a record's official PDF: id plus "
                + "recordType). Upload is refused here — no file bytes travel with a connector "
                + "call. Show the user the full envelope and body and get their explicit yes "
                + "before the confirm: true call: the email goes the moment it succeeds."),
    };
}
