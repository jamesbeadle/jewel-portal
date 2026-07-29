using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.RecordLinks;

// Programme → Communications "Reply in thread": the reply written in the portal is staged as an
// Outlook reply-all draft on the email in the projects mailbox — same mechanics as the triage
// ReplyInThreadFromMessage, but no record is created in the background: the email is already
// triaged into the programme bucket, and the draft carries the bucket's tag ("JPMS/SCH-<projectRef>")
// so the sent copy — and any counter-replies, once triaged — flow straight back into the
// Communications list, keeping the paper trail in one place. Nothing is sent: the pre-filled draft
// waits in the mailbox's Drafts folder until a person reviews and sends it from Outlook.
// InternetMessageId lets the mailbox re-find the message if its Graph id changed.
public sealed record PrepareProgrammeReplyDraft(
    string ProjectId,
    string MessageId,
    string ReplyBody,
    string? InternetMessageId = null) : ICommand<ProgrammeReplyDraft>;
