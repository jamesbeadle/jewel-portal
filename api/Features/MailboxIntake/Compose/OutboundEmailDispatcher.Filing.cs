namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

/// <summary>
/// Where a dispatched email belongs: the pathway its thread is born on, the record it is filed
/// under, and the sentence the person reads when the mailbox will not take the draft at all —
/// written at the door they pressed, because only that door knows what they should do instead.
/// </summary>
public sealed record OutboundEmailFiling(
    string PathwayLabel,
    string StagingRefusal,
    string? ProjectId = null,
    RecordType? RecordType = null,
    string? RecordId = null,
    string RecordReference = "");

/// <summary>
/// What became of it. Sent=false with no FailureNote is a draft left in the mailbox by choice;
/// Sent=false with one is a send the mailbox refused, and the draft is still there to finish.
/// Subject, To and Cc are the envelope as it actually went — which for a reply is Graph's, not the
/// caller's, so a caller reports what the correspondent will see rather than what it asked for.
/// </summary>
public sealed record OutboundEmailDispatch(
    string MessageId,
    string? WebLink,
    bool Sent,
    string? FailureNote,
    string Subject = "",
    IReadOnlyList<string>? To = null,
    IReadOnlyList<string>? Cc = null);
