using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Storage;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Forms.Retention;

public sealed partial class FormRetentionSweep
{
    private const string AbandonedReason = "an upload whose form was never sent, eighteen months after it arrived";

    private async Task<List<RightToWorkCheckEntity>> DueChecksAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var ended = await context.RightToWorkChecks.Where(row => row.EngagementEndedOn != null).ToListAsync(cancellationToken);
        return ended.Where(check => FormRetention.CheckDestroyOn(check.EngagementEndedOn) <= today).ToList();
    }

    private async Task<List<TrainingRecordEntity>> DueCertificatesAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var ended = await context.TrainingRecords.Where(row => row.EndedOn != null).ToListAsync(cancellationToken);
        return ended.Where(record => FormRetention.CertificateDestroyOn(record.EndedOn) <= today).ToList();
    }

    private async Task<List<FormUploadEntity>> AbandonedUploadsAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var cutOff = new DateTimeOffset(today.AddMonths(-FormRetention.AbandonedUploadMonths).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        return await context.FormUploads
            .Where(row => row.FormSubmissionId == null && row.SessionId != FormUploadFolders.OfficeSession
                && row.DeletedAt == null && row.UploadedAt < cutOff)
            .ToListAsync(cancellationToken);
    }

    private async Task<int> ClearDeadLinksAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var cutOff = new DateTimeOffset(today.AddMonths(-FormRetention.UnusedLinkMonths).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var deadInvites = await context.FormInvites
            .Where(row => row.UsedAt == null && (row.ExpiresAt < cutOff || row.CancelledAt < cutOff))
            .ToListAsync(cancellationToken);
        context.FormInvites.RemoveRange(deadInvites);
        return deadInvites.Count;
    }

    private static string RightToWorkReason(RightToWorkCheckEntity check) =>
        $"right to work evidence, two years after the engagement ended on {check.EngagementEndedOn:yyyy-MM-dd}";

    private static string CertificateReason(TrainingRecordEntity record) =>
        $"a training certificate, six years after its holder left on {record.EndedOn:yyyy-MM-dd}";
}
