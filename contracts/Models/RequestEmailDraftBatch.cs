namespace Jewel.JPMS.Models;

/// <summary>
/// One request's outcome within a bulk draft run: either the created draft (see
/// <see cref="RequestEmailDraft"/>) or the user-fixable reason it couldn't be created — a missing
/// recipient, an unknown id, a mailbox hiccup. <see cref="Reference"/> carries the register
/// reference (e.g. RFI-002) so reporting doesn't depend on the caller's cache.
/// </summary>
public sealed record RequestEmailDraftOutcome(
    string RequestId,
    string? Reference,
    RequestEmailDraft? Draft,
    string? Error)
{
    public bool Succeeded => Draft is not null;
}

/// <summary>
/// The result of preparing email drafts for several requests in one go. Partial success is the
/// expected shape: every requested id gets an outcome, drafts that could be created are in the
/// mailbox's Drafts folder, and the failures explain themselves so the person can fix and retry
/// just those.
/// </summary>
public sealed record RequestEmailDraftBatch(IReadOnlyList<RequestEmailDraftOutcome> Outcomes)
{
    public int PreparedCount => Outcomes.Count(outcome => outcome.Succeeded);
    public int FailedCount => Outcomes.Count(outcome => !outcome.Succeeded);
}
