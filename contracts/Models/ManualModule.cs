namespace Jewel.JPMS.Models;

/// <summary>
/// The site manual as controlled modules (2026-09-29, Nigel's Site Manuals and Operating Systems
/// request): one master per module, maintained by the office, with an owner, an approver, a status,
/// a version and a review date. What the site sees is the last APPROVED version; a draft in
/// progress never reaches a site view until it is approved, and approving it supersedes the last.
/// </summary>
public enum ManualModuleStatus
{
    Draft = 0,
    InReview = 1,
    Approved = 2,
    Superseded = 3,
}

/// <summary>The published views of the manual: the office master and the three role views.</summary>
public enum ManualView
{
    Office = 0,
    SiteManager = 1,
    HealthAndSafetyOfficer = 2,
    Foreman = 3,
}

/// <summary>Which role views a module is published to. The office master always shows every module.</summary>
public sealed record ManualAudience(bool IsForSiteManagers, bool IsForHealthAndSafetyOfficer, bool IsForForemen)
{
    public static readonly ManualAudience Everyone = new(true, true, true);

    public bool Includes(ManualView view) => view switch
    {
        ManualView.SiteManager => IsForSiteManagers,
        ManualView.HealthAndSafetyOfficer => IsForHealthAndSafetyOfficer,
        ManualView.Foreman => IsForForemen,
        _ => true,
    };
}

/// <summary>One module of the manual as the office master sees it: the working text and its controls.</summary>
public sealed record ManualModule(
    string ManualModuleId, string Code, string Title, string Purpose, string Body,
    string OwnerEmail, string ApproverEmail, ManualModuleStatus Status,
    int Version, int PublishedVersion, DateTimeOffset? ApprovedAt, string ApprovedByEmail, DateTimeOffset? NextReviewAt,
    ManualAudience Audience, IReadOnlyList<string> LinkedFormSlugs, string LinkedStandards, string ChangeSummary,
    string SourceSections, int Sequence, string UpdatedByEmail, DateTimeOffset UpdatedAt, int AcknowledgedCount)
{
    public bool HasPublishedVersion => PublishedVersion > 0;

    public bool IsBeingRevised => HasPublishedVersion && Status is ManualModuleStatus.Draft or ManualModuleStatus.InReview;

    public bool IsRetired => Status == ManualModuleStatus.Superseded;

    public bool HasLinkedForms => LinkedFormSlugs.Count > 0;

    public string Family => ManualModuleCodes.FamilyOf(Code);
}
