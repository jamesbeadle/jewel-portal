namespace Jewel.JPMS.Models;

/// <summary>
/// One request's outcome within a bulk run: either what became of its email (see
/// <see cref="RequestEmailOutcome"/>, whose Sent says whether it went or is waiting in Drafts) or
/// the user-fixable reason there was no email at all — a missing recipient, an unknown id, a
/// mailbox hiccup. <see cref="Reference"/> carries the register reference (e.g. RFI-002) so
/// reporting doesn't depend on the caller's cache.
/// </summary>
public sealed record RequestEmailResult(
    string RequestId,
    string? Reference,
    RequestEmailOutcome? Email,
    string? Error)
{
    public bool Succeeded => Email is not null;
}

/// <summary>
/// The result of emailing several requests' documents in one go. Partial success is the expected
/// shape: every requested id gets an outcome, and the failures explain themselves so the person
/// can fix and retry just those.
/// </summary>
public sealed record RequestEmailBatch(IReadOnlyList<RequestEmailResult> Outcomes)
{
    public int PreparedCount => Outcomes.Count(outcome => outcome.Succeeded);
    public int SentCount => Outcomes.Count(outcome => outcome.Email is { Sent: true });
    public int FailedCount => Outcomes.Count(outcome => !outcome.Succeeded);
}
