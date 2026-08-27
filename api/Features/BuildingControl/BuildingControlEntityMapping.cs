using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.BuildingControl;

internal static class BuildingControlEntityMapping
{
    public static BuildingControlCase ToModel(this BuildingControlCaseEntity entity) => new(
        entity.BuildingControlCaseId,
        entity.ProjectId,
        entity.Number,
        entity.Reference,
        (BuildingControlRegime)entity.Regime,
        entity.BodyName,
        entity.BodyReference,
        entity.ContactName,
        entity.ContactEmail,
        entity.ContactPhone,
        (BuildingControlCaseStatus)entity.Status,
        entity.NoticeSubmittedOn,
        entity.AcceptedOn,
        entity.CompletionCertifiedOn,
        entity.Notes,
        entity.CreatedByEmail,
        entity.CreatedAt);

    public static BuildingControlInspection ToModel(this BuildingControlInspectionEntity entity) => new(
        entity.BuildingControlInspectionId,
        entity.BuildingControlCaseId,
        entity.ProjectId,
        entity.Number,
        entity.Reference,
        entity.StageName,
        (BuildingControlInspectionStatus)entity.Status,
        entity.BookedFor,
        entity.InspectedAt,
        entity.OutcomeNotes,
        entity.InspectorName,
        entity.DisplayOrder,
        entity.RaisedByEmail,
        entity.RaisedAt);

    public static BuildingControlAttachment ToModel(this BuildingControlAttachmentEntity entity) => new(
        entity.BuildingControlAttachmentId,
        entity.ProjectId,
        entity.BuildingControlCaseId,
        entity.BuildingControlInspectionId,
        (BuildingControlAttachmentKind)entity.Kind,
        entity.FileName,
        entity.ContentType,
        entity.FileSizeBytes,
        (BuildingControlAttachmentSource)entity.Source,
        entity.AddedAt,
        entity.AddedByEmail);
}
