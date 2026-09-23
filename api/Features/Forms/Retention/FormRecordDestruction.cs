using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Storage;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Forms.Retention;

/// <summary>
/// Carrying a destruction out, not scheduling it: the files are deleted from their store, the answers
/// cleared, and the rows that named the person removed — leaving a tombstone that says what was
/// destroyed, when and under which rule, and one line on the audit trail. Nothing here names the person.
/// </summary>
internal sealed class FormRecordDestruction
{
    public const string SweepActor = "Retention sweep";
    private const string Destroyed = "(destroyed)";
    private readonly JpmsContext context;
    private readonly IFormEvidenceStore store;
    private readonly DateTimeOffset now;

    public FormRecordDestruction(JpmsContext context, IFormEvidenceStore store, DateTimeOffset now)
    {
        this.context = context;
        this.store = store;
        this.now = now;
    }

    /// <summary>Files that were already missing from their store when their date came — expected rarely, a sign of a wrong storage setting often.</summary>
    public int FilesAlreadyGone { get; private set; }

    public async Task DestroyFormAsync(FormSubmissionEntity submission, string reason, CancellationToken cancellationToken)
    {
        var id = submission.FormSubmissionId;
        await DeleteFilesAsync(await context.FormUploads.Where(row => row.FormSubmissionId == id).ToListAsync(cancellationToken), reason, cancellationToken);
        context.WorkstationActions.RemoveRange(await context.WorkstationActions.Where(row => row.FormSubmissionId == id).ToListAsync(cancellationToken));
        context.DrivingLicenceChecks.RemoveRange(await context.DrivingLicenceChecks.Where(row => row.FormSubmissionId == id).ToListAsync(cancellationToken));
        context.FormInvites.RemoveRange(await context.FormInvites.Where(row => row.FormSubmissionId == id).ToListAsync(cancellationToken));
        var title = FormCatalogue.TitleOf(submission.FormSlug);
        Record($"{title} sent {submission.SubmittedAt:yyyy-MM-dd} destroyed: {reason}", id);
        Tombstone(submission);
    }

    public async Task DestroyCheckAsync(RightToWorkCheckEntity check, string reason, CancellationToken cancellationToken)
    {
        var folder = FormUploadFolders.RightToWorkEvidence(check.RightToWorkCheckId);
        var evidence = await context.FormUploads.Where(row => row.BlobRef.StartsWith(folder)).ToListAsync(cancellationToken);
        await DeleteFilesAsync(evidence, reason, cancellationToken);
        context.RightToWorkChecks.Remove(check);
        Record($"Right to work check of {check.CheckedOn:yyyy-MM-dd} destroyed: {reason}", check.RightToWorkCheckId);
    }

    public async Task DestroyCertificateAsync(TrainingRecordEntity record, string reason, CancellationToken cancellationToken)
    {
        var certificate = await context.FormUploads.Where(row => row.FormUploadId == record.CertificateUploadId).ToListAsync(cancellationToken);
        await DeleteFilesAsync(certificate, reason, cancellationToken);
        context.TrainingRecords.Remove(record);
        Record($"Training record ended {record.EndedOn:yyyy-MM-dd} destroyed: {reason}", record.TrainingRecordId);
    }

    public Task DestroyAbandonedAsync(FormUploadEntity upload, string reason, CancellationToken cancellationToken) =>
        DeleteFilesAsync(new[] { upload }, reason, cancellationToken);

    private async Task DeleteFilesAsync(IEnumerable<FormUploadEntity> uploads, string reason, CancellationToken cancellationToken)
    {
        foreach (var upload in uploads.Where(upload => upload.DeletedAt is null))
        {
            var wasThere = await store.DeleteAsync((FormEvidenceStore)upload.Store, upload.BlobRef, cancellationToken);
            FilesAlreadyGone += wasThere ? 0 : 1;
            upload.DeletedAt = now;
            upload.DeletionReason = reason.Length > 256 ? reason[..256] : reason;
            upload.FileName = Destroyed;
            upload.BlobRef = "";
            upload.ClientHash = "";
        }
    }

    private void Tombstone(FormSubmissionEntity submission)
    {
        submission.AnswersJson = "{}";
        submission.SubmitterName = Destroyed;
        submission.FilingName = Destroyed;
        submission.SentToEmail = "";
        submission.SentByName = "";
        submission.ClientHash = "";
        submission.Status = (int)FormSubmissionStatus.Destroyed;
        submission.DestroyedAt = now;
    }

    private void Record(string detail, string recordId) =>
        context.AuditEvents.Add(FormAuditRecords.Of(AuditEventType.FormRecordsDestroyed, recordId, SweepActor, detail));
}
