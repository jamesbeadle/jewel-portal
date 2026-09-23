using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Features.Forms.Office;

/// <summary>
/// The check being written in the Record a check dialog, field for field as the dashboard's form
/// laid it out: dates held as the date boxes send them, and the follow-up worked out from the expiry
/// the moment it is typed — ten weeks before, as the register always did.
/// </summary>
public sealed class RightToWorkCheckDraft
{
    private string permissionExpiresOn = "";

    public string? RightToWorkCheckId { get; set; }
    public string? FormSubmissionId { get; init; }
    public string PersonName { get; set; } = "";
    public string Email { get; set; } = "";
    public JewelCompany? Company { get; set; }
    public string JobRole { get; set; } = "";
    public Engagement? EngagedAs { get; set; }
    public string EngagedSince { get; set; } = "";
    public RightToWorkRoute? Route { get; set; }
    public string Reference { get; set; } = "";
    public string IdspProvider { get; set; } = "";
    public RightToWorkSeenVia SeenVia { get; set; }
    public string DocumentReference { get; set; } = "";
    public string CheckedByName { get; set; } = "";
    public string CheckedOn { get; set; } = "";
    public bool IsLikenessConfirmed { get; set; }
    public bool IsDocumentGenuine { get; set; }
    public bool IsPermittedToDoTheWork { get; set; }
    public bool IsEvidenceFiled { get; set; }
    public bool IsTimeLimited { get; set; }
    public string FollowUpOn { get; set; } = "";
    public RightToWorkOutcome Outcome { get; set; }
    public string Notes { get; set; } = "";
    public string EngagementEndedOn { get; set; } = "";

    public string PermissionExpiresOn
    {
        get => permissionExpiresOn;
        set
        {
            permissionExpiresOn = value;
            var expires = FormDates.Read(value);
            FollowUpOn = FormDates.Write(RightToWorkRules.FollowUpFor(expires));
            IsTimeLimited = IsTimeLimited || expires is not null;
        }
    }

    public bool NeedsAReference => Route is { } route && route != RightToWorkRoute.OriginalPassportSeen;

    /// <summary>The register's first refusal, in its order: who, which company, how engaged, then the check's own rules.</summary>
    public string? FirstProblem(DateOnly today) =>
        string.IsNullOrWhiteSpace(PersonName) ? RightToWorkRules.WhoIsBeingEngaged
        : Company is null ? RightToWorkRules.WhichCompany
        : EngagedAs is null ? RightToWorkRules.HowEngaged
        : Route is null ? RightToWorkRules.WhichRoute
        : RightToWorkRules.ProblemsWith(ToDetails(), today).FirstOrDefault();

    public RightToWorkCheckDetails ToDetails() => new(
        PersonName.Trim(), Email.Trim(), Company!.Value, JobRole.Trim(), EngagedAs!.Value, FormDates.Read(EngagedSince), Route!.Value, Reference.Trim(),
        IdspProvider.Trim(), SeenVia, DocumentReference.Trim(), CheckedByName.Trim(), FormDates.Read(CheckedOn) ?? default,
        IsDocumentGenuine, IsLikenessConfirmed, IsPermittedToDoTheWork, IsEvidenceFiled, IsTimeLimited,
        FormDates.Read(PermissionExpiresOn), FormDates.Read(FollowUpOn), Outcome, Notes.Trim(), FormSubmissionId);

    public SaveRightToWorkCheck ToCommand() => new(RightToWorkCheckId, ToDetails(), FormDates.Read(EngagementEndedOn));

    public static RightToWorkCheckDraft Of(RightToWorkCheckDetails details, string? rightToWorkCheckId, DateOnly? engagementEndedOn) => new()
    {
        RightToWorkCheckId = rightToWorkCheckId, FormSubmissionId = details.FormSubmissionId,
        PersonName = details.PersonName, Email = details.Email, Company = details.Company, JobRole = details.JobRole,
        EngagedAs = details.EngagedAs, EngagedSince = FormDates.Write(details.EngagedSince), Route = details.Route,
        Reference = details.Reference, IdspProvider = details.IdspProvider, SeenVia = details.SeenVia,
        DocumentReference = details.DocumentReference, CheckedByName = details.CheckedByName, CheckedOn = FormDates.Write(details.CheckedOn),
        IsLikenessConfirmed = details.IsLikenessConfirmed, IsDocumentGenuine = details.IsDocumentGenuine,
        IsPermittedToDoTheWork = details.IsPermittedToDoTheWork, IsEvidenceFiled = details.IsEvidenceFiled,
        IsTimeLimited = details.IsTimeLimited, PermissionExpiresOn = FormDates.Write(details.PermissionExpiresOn),
        FollowUpOn = FormDates.Write(details.FollowUpOn), Outcome = details.Outcome, Notes = details.Notes,
        EngagementEndedOn = FormDates.Write(engagementEndedOn)
    };
}
