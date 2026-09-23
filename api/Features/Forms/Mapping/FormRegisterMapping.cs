using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Forms.Mapping;

internal static class FormRegisterMapping
{
    public static RightToWorkCheck ToModel(this RightToWorkCheckEntity entity, string evidenceFileName) => new(
        entity.RightToWorkCheckId, entity.DetailsOf(), entity.EvidenceUploadId, evidenceFileName, entity.RecordedByEmail,
        entity.RecordedAt, entity.EngagementEndedOn, entity.ConfirmedOn);

    public static RightToWorkCheckDetails DetailsOf(this RightToWorkCheckEntity entity) => new(
        entity.PersonName, entity.Email, entity.JobRole, (Engagement)entity.EngagedAs,
        entity.EngagedSince, (RightToWorkRoute)entity.Route, entity.Reference, entity.IdspProvider,
        (RightToWorkSeenVia)entity.SeenVia, entity.DocumentReference, entity.CheckedByName, entity.CheckedOn,
        entity.IsDocumentGenuine, entity.IsLikenessConfirmed, entity.IsPermittedToDoTheWork, entity.IsEvidenceFiled,
        entity.IsTimeLimited, entity.PermissionExpiresOn, entity.FollowUpOn, (RightToWorkOutcome)entity.Outcome,
        entity.Notes, entity.FormSubmissionId);

    public static TrainingRecord ToModel(this TrainingRecordEntity entity) => new(
        entity.TrainingRecordId, entity.PersonName, entity.Email, entity.Course,
        entity.Provider, entity.CertificateNumber, entity.CompletedOn, entity.ExpiresOn, entity.FormSubmissionId,
        entity.CertificateUploadId, entity.LastChasedAt, entity.ChaseCount, entity.EndedOn);

    public static WorkstationAction ToModel(this WorkstationActionEntity entity) => new(
        entity.WorkstationActionId, entity.FormSubmissionId, entity.PersonName, entity.Workstation, entity.QuestionKey,
        entity.Action, (WorkstationActionState)entity.State, entity.Note, entity.ResolvedByEmail, entity.ResolvedAt,
        entity.RaisedAt);

    public static DrivingLicenceCheck ToModel(this DrivingLicenceCheckEntity entity) => new(
        entity.DrivingLicenceCheckId, entity.FormSubmissionId, entity.DvlaCheckedOn, entity.IsWithinInsuranceCriteria,
        entity.Note, entity.CheckedByEmail, entity.CheckedAt, entity.PhotosDeleted);
}
