using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

/// <summary>
/// A record whose email the portal writes for it, rather than asking a person to type one.
///
/// The door that SENDS and the read that PREVIEWS both come through here, which is the whole point:
/// what a person reads before pressing Send is the message that will be staged, not a second
/// spelling of it. Adding a fifth such record means adding a composer beside its send handler and
/// registering it — the preview then covers it without being touched.
/// </summary>
public interface IComposesRecordEmail
{
    /// <summary>The kind of record this composes for; how the preview finds it.</summary>
    RecordType Record { get; }

    /// <summary>Who may send it — and therefore who may read what it says, because previewing an
    /// email you could not send tells you about correspondence that is not yours.</summary>
    RoleSet RolesThatMaySend { get; }

    Task<ComposedRecordEmail> ComposeAsync(
        string recordId, string? recipientOverride, CancellationToken cancellationToken);
}

/// <summary>
/// The staged message plus the two things a caller needs that are not on it: the record's own
/// reference, for the audit row and the page, and the project it belongs to (null for a record
/// that belongs to none, like a subcontractor's statement).
///
/// CopiedTo is who the person is TOLD is copied, which is not always the message's Cc — the Graph
/// client copies the projects mailbox onto every message itself, so the request door names it here
/// rather than putting it on the draft twice.
/// </summary>
public sealed record ComposedRecordEmail(
    string Reference,
    string? ProjectId,
    MailboxDraftMessage Message,
    IReadOnlyList<string> CopiedTo);
