using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Office.Submissions;
using Jewel.JPMS.Api.Features.Forms.Retention;
using Jewel.JPMS.Contracts.Forms;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static Jewel.JPMS.Tests.PublicFormFixture;

namespace Jewel.JPMS.Tests;

// The forms' retention is carried out, not proposed (the goal's "a destruction actually carried
// out"): on its date a form's files leave their store, its answers are cleared, the row stays as a
// tombstone naming nobody, the audit trail says what went and under which rule, and a folder with
// nothing left in it goes too. A clock starts only on a recorded date.
public sealed class FormRetentionSweepTests
{
    private static readonly DateOnly Today = new(2026, 9, 23);

    [Fact]
    public async Task RightToWorkEvidence_isDestroyedTwoYearsAfterTheEngagementEnded()
    {
        await using var forms = new PublicFormFixture();
        await SendRightToWorkAsync(forms, sentYearsAgo: 3);
        var folder = await forms.Context.FormFolders.SingleAsync();
        folder.EngagementEndedOn = Today.AddMonths(-FormRetention.RightToWorkMonths).AddDays(-1);
        await forms.Context.SaveChangesAsync();
        var upload = await forms.Context.FormUploads.SingleAsync();
        var blobRef = upload.BlobRef;

        var outcome = await new FormRetentionSweep(forms.Context, forms.Store).RunAsync(Today, CancellationToken.None);

        Assert.Equal(1, outcome.Forms);
        Assert.Equal(0, outcome.FilesAlreadyGone);
        var tombstone = await forms.Context.FormSubmissions.SingleAsync();
        Assert.Equal("{}", tombstone.AnswersJson);
        Assert.Equal((int)FormSubmissionStatus.Destroyed, tombstone.Status);
        Assert.DoesNotContain("Sam", tombstone.SubmitterName + tombstone.FilingName);
        Assert.NotNull(upload.DeletedAt);
        Assert.Equal("", upload.BlobRef);
        Assert.False(forms.Store.Holds(FormEvidenceStore.RightToWork, blobRef));
        var record = await forms.Context.AuditEvents.SingleAsync(row => row.EventType == (int)AuditEventType.FormRecordsDestroyed);
        Assert.DoesNotContain("Sam", record.Detail);
        Assert.False(await forms.Context.FormFolders.AnyAsync());
    }

    [Fact]
    public async Task OneLeavingDateOnTheFolder_clocksTheCheckMadeFromTheirForm_andBothGoOnTheDay()
    {
        await using var forms = new PublicFormFixture();
        await SendRightToWorkAsync(forms, sentYearsAgo: 3);
        var submission = await forms.Context.FormSubmissions.SingleAsync();
        var check = new RightToWorkCheckEntity { RightToWorkCheckId = "check-1", FormSubmissionId = submission.FormSubmissionId, PersonName = "Sam Smith" };
        forms.Context.RightToWorkChecks.Add(check);
        await forms.Context.SaveChangesAsync();
        var endedOn = Today.AddMonths(-FormRetention.RightToWorkMonths);

        await new RecordFormFolderDatesHandler(forms.Context).HandleAsync(
            new RecordFormFolderDates(submission.FormFolderId!, endedOn, null), CancellationToken.None);
        var outcome = await new FormRetentionSweep(forms.Context, forms.Store).RunAsync(Today, CancellationToken.None);

        Assert.Equal(endedOn, check.EngagementEndedOn);
        Assert.Equal(1, outcome.Forms);
        Assert.Equal(1, outcome.Checks);
        Assert.False(await forms.Context.RightToWorkChecks.AnyAsync());
    }

    [Fact]
    public async Task AFormSentAfterTheRecordedLeavingDate_isNotClockedByIt()
    {
        await using var forms = new PublicFormFixture();
        await SendRightToWorkAsync(forms, sentYearsAgo: 0);
        var folder = await forms.Context.FormFolders.SingleAsync();
        folder.EngagementEndedOn = Today.AddYears(-4);
        await forms.Context.SaveChangesAsync();

        var outcome = await new FormRetentionSweep(forms.Context, forms.Store).RunAsync(Today, CancellationToken.None);

        Assert.Equal(0, outcome.Forms);
    }

    [Fact]
    public async Task AFormWhosePersonHasNoRecordedLeavingDate_isNeverClocked()
    {
        await using var forms = new PublicFormFixture();
        await SendRightToWorkAsync(forms, sentYearsAgo: 3);

        var outcome = await new FormRetentionSweep(forms.Context, forms.Store).RunAsync(Today.AddYears(10), CancellationToken.None);

        Assert.Equal(0, outcome.Forms);
        Assert.Contains("Sam Smith", (await forms.Context.FormSubmissions.SingleAsync()).AnswersJson);
    }

    [Fact]
    public async Task AnUploadWhoseFormWasNeverSent_goesEighteenMonthsAfterItArrived()
    {
        await using var forms = new PublicFormFixture();
        var drawing = await forms.SignAsync(FirstSession);
        var upload = await forms.Context.FormUploads.SingleAsync(row => row.FormUploadId == drawing.FormUploadId);
        var arrivedOn = Today.AddMonths(-FormRetention.AbandonedUploadMonths).AddDays(-1);
        upload.UploadedAt = new DateTimeOffset(arrivedOn.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        await forms.Context.SaveChangesAsync();
        var blobRef = upload.BlobRef;

        var outcome = await new FormRetentionSweep(forms.Context, forms.Store).RunAsync(Today, CancellationToken.None);

        Assert.Equal(1, outcome.AbandonedUploads);
        Assert.NotNull(upload.DeletedAt);
        Assert.False(forms.Store.Holds(FormEvidenceStore.RightToWork, blobRef));
    }

    private static async Task SendRightToWorkAsync(PublicFormFixture forms, int sentYearsAgo)
    {
        var drawing = await forms.SignAsync(FirstSession);
        var posted = Posted(FirstSession, RightToWorkAnswers(), drawingId: drawing.FormUploadId);
        await forms.Service.SubmitAsync(FormSlugs.RightToWork, posted, Address, CancellationToken.None);
        var submission = await forms.Context.FormSubmissions.SingleAsync();
        submission.SubmittedAt = new DateTimeOffset(Today.AddYears(-sentYearsAgo).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        await forms.Context.SaveChangesAsync();
    }
}
