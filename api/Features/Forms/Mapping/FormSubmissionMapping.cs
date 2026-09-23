using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Forms.Mapping;

internal static class FormSubmissionMapping
{
    public static FormSubmission ToModel(this FormSubmissionEntity entity) => new(
        entity.FormSubmissionId, entity.FormSlug, (JewelCompany)entity.Company, entity.SubmitterName, entity.FilingName,
        entity.FormFolderId, entity.IsVerifiedLink, entity.SentToEmail, entity.SentByName, entity.FormPackId,
        (FormSubmissionStatus)entity.Status, entity.SubmittedAt, entity.HandledByEmail, entity.HandledAt);

    public static FormUploadedFile ToModel(this FormUploadEntity entity) => new(
        entity.FormUploadId, entity.QuestionKey, entity.FileName, entity.ContentType, entity.Size, entity.DeletedAt,
        entity.DeletionReason);

    public static FormFolder ToModel(this FormFolderEntity entity, int submissionCount) => new(
        entity.FormFolderId, entity.Name, (FormFilingKind)entity.Kind, (JewelCompany)entity.Company, entity.EngagementEndedOn,
        entity.VehicleReturnedOn, entity.LastSubmittedAt, submissionCount);
}
