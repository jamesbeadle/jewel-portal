using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.DocumentControl.Storage;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

// Files a pending Document Control item as a payment certificate on a project. The certificate
// takes its OWN blob copy (queue housekeeping can never orphan the register) and optionally ties
// to the valuation claim it certifies — validated to belong to the same project, because a
// certificate pointing at another job's claim would quietly corrupt the register.
public sealed class FileDocumentAsPaymentCertificateHandler
    : ICommandHandler<FileDocumentAsPaymentCertificate, DocumentControlItem>
{
    private readonly JpmsContext context;
    private readonly IDocumentControlBlobStore blobStore;
    private readonly AuditActor actor;
    private readonly AuditTrail auditTrail;

    public FileDocumentAsPaymentCertificateHandler(
        JpmsContext context, IDocumentControlBlobStore blobStore, AuditActor actor, AuditTrail auditTrail)
    {
        this.context = context; this.blobStore = blobStore; this.actor = actor; this.auditTrail = auditTrail;
    }

    public async Task<DocumentControlItem> HandleAsync(
        FileDocumentAsPaymentCertificate command, CancellationToken cancellationToken)
    {
        var item = await context.DocumentControlItems
            .FirstOrDefaultAsync(row => row.DocumentControlItemId == command.DocumentControlItemId, cancellationToken)
            ?? throw new InvalidOperationException("That document is no longer in Document Triage.");
        if (item.Status != (int)DocumentControlStatus.Pending)
            throw new InvalidOperationException("That document has already been filed or discarded — restore it to the queue first.");

        var project = await context.Projects
            .FirstOrDefaultAsync(row => row.ProjectId == command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException("Select the project this certificate belongs to.");

        if (!string.IsNullOrWhiteSpace(command.ValuationClaimId))
        {
            var claimBelongs = await context.ValuationClaims.AnyAsync(
                row => row.ValuationClaimId == command.ValuationClaimId && row.ProjectId == command.ProjectId,
                cancellationToken);
            if (!claimBelongs)
                throw new InvalidOperationException("That valuation claim doesn't belong to the selected project.");
        }

        var sourceBlob = await blobStore.OpenAsync(item.BlobRef, cancellationToken)
            ?? throw new InvalidOperationException("The stored file could not be found in Document Triage's storage.");

        var certificateId = DocumentControlIdentifierFactory.NextPaymentCertificateId();
        string certificateBlobRef;
        await using (var content = sourceBlob.Content)
        {
            certificateBlobRef = await blobStore.UploadPaymentCertificateAsync(
                command.ProjectId, certificateId, item.FileName, item.ContentType, content, cancellationToken);
        }

        var certificateNumber = command.CertificateNumber.Trim();
        context.PaymentCertificates.Add(new PaymentCertificateEntity
        {
            PaymentCertificateId = certificateId,
            ProjectId = command.ProjectId,
            CertificateNumber = certificateNumber,
            CertifiedAmount = command.CertifiedAmount,
            IssuedDate = command.IssuedDate,
            ValuationClaimId = string.IsNullOrWhiteSpace(command.ValuationClaimId) ? null : command.ValuationClaimId,
            FileName = item.FileName,
            ContentType = item.ContentType,
            FileSizeBytes = item.FileSizeBytes,
            BlobRef = certificateBlobRef,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = actor.Email,
            SourceDocumentControlItemId = item.DocumentControlItemId
        });

        item.Status = (int)DocumentControlStatus.Filed;
        item.ResolvedBy = actor.Email;
        item.ResolvedAt = DateTimeOffset.UtcNow;
        item.FiledAsKind = (int)DocumentFiledAs.PaymentCertificate;
        item.FiledRecordId = certificateId;
        item.FiledLabel = $"Payment Certificate {certificateNumber} on {project.Name}";

        await context.SaveChangesAsync(cancellationToken);

        await auditTrail.WriteAsync(
            AuditEventType.DocumentFiled,
            $"Filed \"{item.FileName}\" from Document Triage as {item.FiledLabel}",
            projectId: command.ProjectId,
            emailMessageId: item.MessageId,
            internetMessageId: item.InternetMessageId,
            cancellationToken: cancellationToken);

        return item.ToModel();
    }
}
