namespace Jewel.JPMS.Models;

// Where the audit as a whole has got to: filled in on site (Draft) → the officer's declaration
// made and the corrective actions minted (Issued) → the manager's declaration that every action
// is done (Closed). Persisted as its integer value; append, never insert.
public enum HsAuditStatus
{
    Draft = 0,
    Issued = 1,
    Closed = 2
}

// The spreadsheet's "Type of Report" list.
public enum HsAuditType
{
    Initial = 0,
    Routine = 1,
    FollowUp = 2,
    Final = 3,
    Other = 4
}

// The rating bands the score falls into (HsAuditScoring.RatingOf).
public enum HsAuditRating
{
    Poor = 0,
    Fair = 1,
    Good = 2,
    VeryGood = 3
}

public static class HsAuditStatuses
{
    public static string DisplayName(this HsAuditStatus status) => status switch
    {
        HsAuditStatus.Draft => "Draft",
        HsAuditStatus.Issued => "Issued",
        HsAuditStatus.Closed => "Closed",
        _ => status.ToString()
    };
}

public static class HsAuditTypes
{
    public static string DisplayName(this HsAuditType type) => type switch
    {
        HsAuditType.Initial => "Initial",
        HsAuditType.Routine => "Routine",
        HsAuditType.FollowUp => "Follow-up",
        HsAuditType.Final => "Final",
        HsAuditType.Other => "Other",
        _ => type.ToString()
    };
}

public static class HsAuditRatings
{
    public static string DisplayName(this HsAuditRating rating) => rating switch
    {
        HsAuditRating.Poor => "Poor (report required)",
        HsAuditRating.Fair => "Fair",
        HsAuditRating.Good => "Good",
        HsAuditRating.VeryGood => "Very good",
        _ => rating.ToString()
    };
}

/// <summary>
/// One run of the H&S inspection framework on a project — the header of the officer's report.
/// Number is minted per project (the work-order rule) and reads as HSA-0001. Score is the
/// spreadsheet's figure (HsAuditScoring), recomputed on every item write; null until an item
/// has been rated. PreviousScore is the last issued audit's score on the project at the time
/// this one was created.
/// </summary>
public sealed record HsAudit(
    string HsAuditId,
    string ProjectId,
    int Number,
    string Reference,
    HsAuditStatus Status,
    HsAuditType Type,
    DateTimeOffset InspectionDate,
    string SiteManagerName,
    string SafetyOfficerName,
    string SummaryOfWorkActivities,
    int? SiteOperativeCount,
    string FurtherComments,
    decimal? Score,
    decimal? PreviousScore,
    string TemplateVersion,
    string ManagerName,
    DateTimeOffset? IssuedAt,
    DateTimeOffset? ClosedAt,
    string CreatedByEmail,
    DateTimeOffset CreatedAt)
{
    public HsAuditRating? Rating => Score is { } score ? HsAuditScoring.RatingOf(score) : null;
}
