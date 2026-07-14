using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// Triage "Reply in thread": stage an Outlook reply draft on the email (in the projects mailbox,
// with the whole thread quoted behind it — same mechanics as PrepareRequestReplyDraft, but with no
// document attached) AND create a General request from the email in the background, so the act of
// replying IS the triage — the email leaves the queue tagged to the new request, and the request's
// description records that it was answered by a reply in the thread. Nothing is sent: the triager
// writes and sends the reply from the mailbox itself. RaisedByEmail is stamped server-side from the
// signed-in triager. InternetMessageId lets the mailbox re-find the message if its Graph id changed.
public sealed record ReplyInThreadFromMessage(
    string MessageId,
    string ProjectId,
    string? InternetMessageId = null,
    string RaisedByEmail = "") : ICommand<ReplyInThreadOutcome>;
