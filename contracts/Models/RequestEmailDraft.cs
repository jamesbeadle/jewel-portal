namespace Jewel.JPMS.Models;

/// <summary>
/// The outcome of preparing an email draft for a request's official document: where the draft went
/// and who it is addressed to. <see cref="WebLink"/> opens the draft in Outlook on the web when
/// Graph returns one (it usually does); null otherwise — the draft is still in the Drafts folder.
/// Cc/Bcc list the copied recipients the draft carries; showing Bcc here is correct because the
/// person reviewing the draft is internal (Bcc stays off every client-facing surface).
/// </summary>
public sealed record RequestEmailDraft(
    string RequestId,
    string Subject,
    IReadOnlyList<string> Recipients,
    string? WebLink,
    IReadOnlyList<string>? Cc = null,
    IReadOnlyList<string>? Bcc = null);
