namespace Jewel.JPMS.Models;

/// <summary>
/// The outcome of preparing an email draft for a request's official document: where the draft went
/// and who it is addressed to. <see cref="WebLink"/> opens the draft in Outlook on the web when
/// Graph returns one (it usually does); null otherwise — the draft is still in the Drafts folder.
/// </summary>
public sealed record RequestEmailDraft(
    string RequestId,
    string Subject,
    IReadOnlyList<string> Recipients,
    string? WebLink);
