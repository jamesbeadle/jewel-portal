namespace Jewel.JPMS.Models;

/// <summary>
/// The outcome of the triage "Reply in thread" action: the General request created in the background
/// (the email is already tagged to it) and the reply draft staged in the projects mailbox. The
/// draft's <see cref="RequestEmailDraft.WebLink"/> opens it in Outlook on the web for the triager to
/// write and send — nothing has been sent yet.
/// </summary>
public sealed record ReplyInThreadOutcome(
    Request Request,
    RequestEmailDraft Draft);
