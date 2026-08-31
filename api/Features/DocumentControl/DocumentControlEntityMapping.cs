using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.DocumentControl;

public static class DocumentControlEntityMapping
{
    public static DocumentControlItem ToModel(this DocumentControlItemEntity entity) => new(
        entity.DocumentControlItemId,
        entity.MessageId,
        entity.InternetMessageId,
        entity.AttachmentId,
        entity.FromEmail,
        entity.FromName,
        entity.Subject,
        entity.ReceivedAt,
        entity.FileName,
        entity.ContentType,
        entity.FileSizeBytes,
        entity.ProjectIdHint,
        (DocumentControlStatus)entity.Status,
        entity.SentBy,
        entity.SentAt,
        entity.ResolvedBy,
        entity.ResolvedAt,
        entity.FiledAsKind is { } kind ? (DocumentFiledAs)kind : null,
        entity.FiledRecordId,
        entity.FiledLabel,
        entity.SourceDocumentControlItemId);

    public static PaymentCertificate ToModel(this PaymentCertificateEntity entity) => new(
        entity.PaymentCertificateId,
        entity.ProjectId,
        entity.CertificateNumber,
        entity.CertifiedAmount,
        entity.IssuedDate,
        entity.ValuationClaimId,
        entity.FileName,
        entity.ContentType,
        entity.FileSizeBytes,
        entity.CreatedAt,
        entity.CreatedBy,
        entity.SourceDocumentControlItemId);
}
