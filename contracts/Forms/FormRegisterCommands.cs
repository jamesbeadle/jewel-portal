using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Forms;

/// <summary>
/// Records (or corrects) a right-to-work check against its named checker. EngagementEndedOn starts
/// the two-year retention clock when the person leaves. RecordedByEmail is stamped server-side.
/// </summary>
public sealed record SaveRightToWorkCheck(
    string? RightToWorkCheckId,
    RightToWorkCheckDetails Details,
    DateOnly? EngagementEndedOn = null,
    string RecordedByEmail = "") : ICommand<RightToWorkCheck>;

/// <summary>
/// Emails the person that their right-to-work check was completed — on what date, by whom and by which
/// route — once it is a completed pass. SentByEmail is stamped server-side.
/// </summary>
public sealed record SendRightToWorkConfirmation(string RightToWorkCheckId, string SentByEmail = "") : ICommand<RightToWorkCheck>;

/// <summary>Accepts a Training Certificate form onto the training register, with the person and course the office picks.</summary>
public sealed record AcceptTrainingCertificate(
    string FormSubmissionId,
    string PersonName,
    string Course,
    DateOnly CompletedOn,
    DateOnly? ExpiresOn,
    string AcceptedByEmail = "") : ICommand<TrainingRecord>;

/// <summary>Corrects a certificate's email (where its renewal is chased), its expiry, or ends it when the person leaves.</summary>
public sealed record SetTrainingRecordDetails(
    string TrainingRecordId,
    string Email,
    DateOnly? ExpiresOn,
    DateOnly? EndedOn) : ICommand<TrainingRecord>;

/// <summary>Marks a workstation action fixed or accepted, with a note of what was done or why it stands.</summary>
public sealed record ResolveWorkstationAction(
    string WorkstationActionId,
    WorkstationActionState State,
    string Note,
    string ResolvedByEmail = "") : ICommand<WorkstationAction>;

/// <summary>
/// Records the office's licence check on a Company Vehicle Form. Deletes the licence photograph
/// and withholds the driving-record answers and the check code — only the outcome is kept.
/// </summary>
public sealed record RecordDrivingLicenceCheck(
    string FormSubmissionId,
    DateOnly DvlaCheckedOn,
    bool IsWithinInsuranceCriteria,
    string Note,
    string CheckedByEmail = "") : ICommand<DrivingLicenceCheck>;
