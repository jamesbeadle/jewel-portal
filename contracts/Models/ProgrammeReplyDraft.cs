namespace Jewel.JPMS.Models;

/// <summary>
/// The outcome of staging a reply draft from Programme → Communications: where the draft went and
/// who it is addressed to. <see cref="WebLink"/> opens the draft in Outlook on the web when Graph
/// returns one (it usually does); null otherwise — the draft is still in the projects mailbox's
/// Drafts folder. Cc lists the copied recipients the reply-all inherited from the original
/// conversation; showing it here is correct because the person reviewing the draft is internal.
/// </summary>
public sealed record ProgrammeReplyDraft(
    string ProjectId,
    string Subject,
    IReadOnlyList<string> Recipients,
    string? WebLink,
    IReadOnlyList<string>? Cc = null);
