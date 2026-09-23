using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Forms.Registers;

/// <summary>A check's details onto its row. The follow-up date defaults to ten weeks before time-limited permission runs out.</summary>
internal static class RightToWorkCheckWriting
{
    private const int LongestNotes = 2000;

    public static void Apply(RightToWorkCheckEntity check, RightToWorkCheckDetails details)
    {
        check.PersonName = details.PersonName.Trim();
        check.Email = details.Email?.Trim() ?? "";
        check.JobRole = details.JobRole?.Trim() ?? "";
        check.EngagedAs = (int)details.EngagedAs;
        check.EngagedSince = details.EngagedSince;
        check.Route = (int)details.Route;
        check.Reference = details.Reference?.Trim() ?? "";
        check.IdspProvider = details.IdspProvider?.Trim() ?? "";
        check.SeenVia = (int)details.SeenVia;
        check.DocumentReference = details.DocumentReference?.Trim() ?? "";
        check.CheckedByName = details.CheckedByName.Trim();
        check.CheckedOn = details.CheckedOn;
        check.IsDocumentGenuine = details.IsDocumentGenuine;
        check.IsLikenessConfirmed = details.IsLikenessConfirmed;
        check.IsPermittedToDoTheWork = details.IsPermittedToDoTheWork;
        check.IsEvidenceFiled = details.IsEvidenceFiled || check.EvidenceUploadId is not null;
        check.IsTimeLimited = details.IsTimeLimited;
        check.PermissionExpiresOn = details.IsTimeLimited ? details.PermissionExpiresOn : null;
        check.FollowUpOn = details.FollowUpOn ?? RightToWorkRules.FollowUpFor(check.PermissionExpiresOn);
        check.Outcome = (int)details.Outcome;
        check.Notes = Clip(details.Notes ?? "", LongestNotes);
        check.FormSubmissionId = string.IsNullOrWhiteSpace(details.FormSubmissionId) ? null : details.FormSubmissionId;
    }

    private static string Clip(string value, int longest) => value.Length > longest ? value[..longest] : value;
}
