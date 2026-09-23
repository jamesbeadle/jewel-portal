using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Storage;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Forms.Retention;

/// <summary>What one night's sweep destroyed, and how many of its files were already gone from their store.</summary>
public sealed record FormRetentionOutcome(int Forms, int Checks, int Certificates, int AbandonedUploads, int DeadLinks, int FilesAlreadyGone);

/// <summary>
/// The forms' retention, carried out nightly (FormRetention's periods, lib/retention.js): every form,
/// right-to-work check, training certificate and abandoned upload whose date has come is destroyed
/// and recorded, and links nobody used are cleared away. Unlike the dashboard's sweep it does not wait
/// for an approval click: the date IS the decision, recorded by the office when the person left.
/// </summary>
public sealed partial class FormRetentionSweep
{
    private readonly JpmsContext context;
    private readonly IFormEvidenceStore store;

    public FormRetentionSweep(JpmsContext context, IFormEvidenceStore store)
    {
        this.context = context;
        this.store = store;
    }

    public async Task<FormRetentionOutcome> RunAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var destruction = new FormRecordDestruction(context, store, DateTimeOffset.UtcNow);
        var forms = await DueFormsAsync(today, cancellationToken);
        await DestroyEachAsync(forms, due => destruction.DestroyFormAsync(due.Submission, due.Reason, cancellationToken), cancellationToken);
        var checks = await DueChecksAsync(today, cancellationToken);
        await DestroyEachAsync(checks, check => destruction.DestroyCheckAsync(check, RightToWorkReason(check), cancellationToken), cancellationToken);
        var certificates = await DueCertificatesAsync(today, cancellationToken);
        await DestroyEachAsync(certificates, record => destruction.DestroyCertificateAsync(record, CertificateReason(record), cancellationToken), cancellationToken);
        var abandoned = await AbandonedUploadsAsync(today, cancellationToken);
        await DestroyEachAsync(abandoned, upload => destruction.DestroyAbandonedAsync(upload, AbandonedReason, cancellationToken), cancellationToken);
        var deadLinks = await ClearDeadLinksAsync(today, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await RemoveEmptyHoldersAsync(cancellationToken);
        return new FormRetentionOutcome(forms.Count, checks.Count, certificates.Count, abandoned.Count, deadLinks, destruction.FilesAlreadyGone);
    }

    /// <summary>Each destruction is saved as it is done, so one file that will not delete never holds back the rest of the night.</summary>
    private async Task DestroyEachAsync<TDue>(IEnumerable<TDue> due, Func<TDue, Task> destroy, CancellationToken cancellationToken)
    {
        foreach (var item in due)
        {
            await destroy(item);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<List<(FormSubmissionEntity Submission, string Reason)>> DueFormsAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var live = await context.FormSubmissions.Where(row => row.DestroyedAt == null).ToListAsync(cancellationToken);
        var folderIds = live.Select(row => row.FormFolderId).OfType<string>().Distinct().ToList();
        var folders = await context.FormFolders.AsNoTracking()
            .Where(row => folderIds.Contains(row.FormFolderId)).ToDictionaryAsync(row => row.FormFolderId, cancellationToken);
        return live
            .Select(submission => (Submission: submission, DestroyOn: DestroyOn(submission, folders)))
            .Where(due => due.DestroyOn is { } date && date <= today)
            .Select(due => (due.Submission, $"kept until {due.DestroyOn:yyyy-MM-dd} under the forms' retention rules"))
            .ToList();
    }

    private static DateOnly? DestroyOn(FormSubmissionEntity submission, IReadOnlyDictionary<string, FormFolderEntity> folders)
    {
        var form = FormCatalogue.For(submission.FormSlug);
        var folder = folders.GetValueOrDefault(submission.FormFolderId ?? "");
        if (form is null || folder is null) return null;
        var sentOn = DateOnly.FromDateTime(submission.SubmittedAt.UtcDateTime);
        var lastSubmittedOn = DateOnly.FromDateTime(folder.LastSubmittedAt.UtcDateTime);
        return FormRetention.DestroyOn(form, sentOn, folder.EngagementEndedOn, folder.VehicleReturnedOn, lastSubmittedOn);
    }

    /// <summary>A folder with no form left, and a pack with no link left, name a person and hold nothing: they go too.</summary>
    private async Task RemoveEmptyHoldersAsync(CancellationToken cancellationToken)
    {
        var foldersInUse = context.FormSubmissions.Where(row => row.DestroyedAt == null && row.FormFolderId != null).Select(row => row.FormFolderId!);
        context.FormFolders.RemoveRange(await context.FormFolders.Where(row => !foldersInUse.Contains(row.FormFolderId)).ToListAsync(cancellationToken));
        var packsInUse = context.FormInvites.Where(row => row.FormPackId != null).Select(row => row.FormPackId!);
        context.FormPacks.RemoveRange(await context.FormPacks.Where(row => !packsInUse.Contains(row.FormPackId)).ToListAsync(cancellationToken));
        await context.SaveChangesAsync(cancellationToken);
    }
}
