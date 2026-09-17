namespace Jewel.JPMS.Models;

/// <summary>
/// What became of a Programme → Communications reply: where it went and who it is addressed to.
/// Sent=true means the thread has it; Sent=false is a draft waiting in the mailbox's Drafts folder
/// — by choice when <see cref="FailureNote"/> is null, because the send was refused when it is not.
/// <see cref="WebLink"/> opens the message in Outlook on the web when Graph returns one. Cc lists
/// the copied recipients the reply-all inherited from the original conversation; showing it here is
/// correct because the person reading it is internal.
/// </summary>
public sealed record ProgrammeReplyOutcome(
    string ProjectId,
    string Subject,
    IReadOnlyList<string> Recipients,
    string? WebLink,
    IReadOnlyList<string>? Cc = null,
    bool Sent = false,
    string? FailureNote = null);
