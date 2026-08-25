using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.TenderEnquiries;

internal static class TenderEnquiryEntityMapping
{
    public static TenderEnquiry ToModel(this TenderEnquiryEntity entity) => new(
        entity.TenderEnquiryId,
        entity.ProjectId,
        entity.Number,
        entity.Title,
        entity.ArchitectPracticeName,
        entity.ArchitectContactName,
        entity.ArchitectContactEmail,
        entity.ScopeSummary,
        entity.ContractForm,
        (TenderEnquiryStatus)entity.Status,
        entity.ReceivedAt,
        entity.PqqDueAt,
        entity.TenderDueAt,
        entity.PqqSubmittedAt,
        entity.TenderSubmittedAt,
        entity.DecidedAt,
        entity.DecisionNote,
        entity.OwnerEmail,
        entity.CreatedAt,
        entity.CreatedByEmail);

    public static TenderEnquiryAnswer ToModel(this TenderEnquiryAnswerEntity entity) => new(
        entity.TenderEnquiryAnswerId,
        entity.TenderEnquiryId,
        entity.Position,
        entity.Question,
        entity.Answer);

    public static TenderEnquiryAttachment ToModel(this TenderEnquiryAttachmentEntity entity) => new(
        entity.TenderEnquiryAttachmentId,
        entity.TenderEnquiryId,
        entity.ProjectId,
        entity.FileName,
        entity.ContentType,
        entity.FileSizeBytes,
        (TenderEnquiryAttachmentSource)entity.Source,
        entity.AddedAt,
        entity.AddedByEmail);
}

internal static class TenderEnquiryIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string Next() => Guid.NewGuid().ToString(CompactGuidFormat);
}
