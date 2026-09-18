using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The projects mailbox as a test can drive it: it remembers the draft it was handed, answers a
/// reply draft with the envelope Graph would have filled in from the original message, and can be
/// told to refuse the send so the degrade path is exercised. Everything the outbound work never
/// touches is refused outright in the sibling partial, so a test that strays says so.
/// </summary>
public sealed partial class RecordingMailbox : IMailboxGraphClient
{
    public const string DraftId = "draft-1";
    public const string DraftWebLink = "https://outlook.example/draft-1";
    public const string SentWebLink = "https://outlook.example/sent-1";

    /// <summary>False makes the mailbox refuse the send, leaving the staged draft behind.</summary>
    public bool SendSucceeds { get; set; } = true;

    /// <summary>Null makes the mailbox refuse to stage anything at all.</summary>
    public MailboxDraft? Staged { get; set; } = new(DraftId, DraftWebLink);

    public MailboxDraftMessage? CreatedDraft { get; private set; }
    public MailboxReplyDraftMessage? CreatedReply { get; private set; }
    public bool WasSent { get; private set; }

    /// <summary>What Graph fills in on a reply draft from the message being answered — the
    /// recipients and subject the correspondent will actually see, which is never what the
    /// caller asked for.</summary>
    public MailboxReplyDraft ReplyDraft { get; set; } = new(
        DraftId, DraftWebLink, "RE: Shower trays",
        new[] { "architect@example.com" },
        new[] { "client@example.com", "projects@jewelbb.co.uk" });

    public Task<MailboxDraft?> CreateDraftAsync(MailboxDraftMessage draft, CancellationToken ct)
    {
        CreatedDraft = draft;
        return Task.FromResult(Staged);
    }

    public Task<MailboxReplyDraft?> CreateReplyDraftAsync(MailboxReplyDraftMessage reply, CancellationToken ct)
    {
        CreatedReply = reply;
        return Task.FromResult(Staged is null ? null : ReplyDraft);
    }

    public Task<bool> SendDraftAsync(string draftMessageId, CancellationToken ct)
    {
        WasSent = SendSucceeds;
        return Task.FromResult(SendSucceeds);
    }

    public Task<string?> GetWebLinkAsync(string messageId, CancellationToken ct) =>
        Task.FromResult<string?>(SentWebLink);
}
