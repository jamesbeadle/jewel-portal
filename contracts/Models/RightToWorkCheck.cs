namespace Jewel.JPMS.Models;

/// <summary>How the check was done. Persisted as ints: append, never reorder.</summary>
public enum RightToWorkRoute
{
    OriginalPassportSeen = 0,
    ShareCode = 1,
    IdentityServiceProvider = 2
}

public enum RightToWorkOutcome
{
    Pass = 0,
    QueryDoNotStart = 1,
    Fail = 2
}

public enum RightToWorkSeenVia
{
    InPerson = 0,
    VideoCall = 1
}

/// <summary>
/// A right-to-work check as the register holds it: the details, the evidence on file, who recorded
/// it, and the day the person was emailed that it was completed.
/// </summary>
public sealed record RightToWorkCheck(
    string RightToWorkCheckId,
    RightToWorkCheckDetails Details,
    string? EvidenceUploadId,
    string EvidenceFileName,
    string RecordedByEmail,
    DateTimeOffset RecordedAt,
    DateOnly? EngagementEndedOn,
    DateOnly? ConfirmedOn = null)
{
    public bool IsCleared => RightToWorkRules.IsCleared(Details);

    public bool IsLate => RightToWorkRules.IsLate(Details);
}

/// <summary>
/// The checker's record — the thing that gives the statutory excuse, which is why it is kept in
/// the office against a named person and a date and not on the public form. The three
/// confirmations are the ones a pass needs: likeness checked live, the document genuine, in date
/// and theirs, and permission to do this work.
/// </summary>
public sealed record RightToWorkCheckDetails(
    string PersonName,
    string Email,
    string JobRole,
    Engagement EngagedAs,
    DateOnly? EngagedSince,
    RightToWorkRoute Route,
    string Reference,
    string IdspProvider,
    RightToWorkSeenVia SeenVia,
    string DocumentReference,
    string CheckedByName,
    DateOnly CheckedOn,
    bool IsDocumentGenuine,
    bool IsLikenessConfirmed,
    bool IsPermittedToDoTheWork,
    bool IsEvidenceFiled,
    bool IsTimeLimited,
    DateOnly? PermissionExpiresOn,
    DateOnly? FollowUpOn,
    RightToWorkOutcome Outcome,
    string Notes,
    string? FormSubmissionId);
