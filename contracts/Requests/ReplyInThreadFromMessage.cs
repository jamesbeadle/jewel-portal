using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// Triage "Reply in thread": the triager writes the reply IN THE PORTAL (ReplyBody), and one action
// stages it as an Outlook reply draft on the email (projects mailbox, written reply above the whole
// quoted thread — same mechanics as PrepareRequestReplyDraft, but the body is the triager's own
// words and no document is attached) AND creates a General request from the email in the
// background, whose description carries that same written reply ("Replied to email in thread
// with: …"). So one write-up both answers the email and papers the request — the act of replying
// IS the triage. Nothing is sent: the pre-filled draft is reviewed and sent from the mailbox
// itself. RaisedByEmail is stamped server-side from the signed-in triager. InternetMessageId lets
// the mailbox re-find the message if its Graph id changed.
public sealed record ReplyInThreadFromMessage(
    string MessageId,
    string ProjectId,
    string ReplyBody,
    string? InternetMessageId = null,
    string RaisedByEmail = "") : ICommand<ReplyInThreadOutcome>;
