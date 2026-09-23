namespace Jewel.JPMS.Models;

/// <summary>An action is finished when it is fixed, or recorded as accepted. Persisted as ints.</summary>
public enum WorkstationActionState
{
    Open = 0,
    Fixed = 1,
    Accepted = 2
}

/// <summary>
/// One thing to sort out from a workstation assessment. Every question is worded so NO is the
/// thing to fix, so the noes across every assessment are the work queue, and an assessment is not
/// finished until each is fixed or accepted.
/// </summary>
public sealed record WorkstationAction(
    string WorkstationActionId,
    string FormSubmissionId,
    string PersonName,
    string Workstation,
    string QuestionKey,
    string Action,
    WorkstationActionState State,
    string Note,
    string ResolvedByEmail,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset RaisedAt);

public sealed record WorkstationActionNeeded(string QuestionKey, string Action);
