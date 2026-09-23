using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Registers;
using Jewel.JPMS.Contracts.Forms;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The Company Vehicle Form's privacy paragraph, kept (the task's "licence photographs: deleted once
// verified"): recording the office's licence check deletes the photograph from its store there and
// then, and the driving-record answers and the DVLA check code are withheld from the stored form for
// good — only whether the person meets the insurance criteria is kept.
public sealed class DrivingLicenceCheckTests
{
    private const string FormId = "vehicle-form-1";

    [Fact]
    public async Task ALicenceCheck_deletesThePhotograph_andWithholdsTheDrivingRecord()
    {
        await using var forms = new PublicFormFixture();
        var (submission, photo) = await VehicleFormWithAPhotoAsync(forms);

        var check = await new RecordDrivingLicenceCheckHandler(forms.Context, forms.Store).HandleAsync(
            new RecordDrivingLicenceCheck(FormId, new DateOnly(2026, 9, 23), true, "", "office@jewelps.co.uk"), CancellationToken.None);

        Assert.Equal(1, check.PhotosDeleted);
        Assert.True(check.IsWithinInsuranceCriteria);
        Assert.NotNull(photo.DeletedAt);
        Assert.False(forms.Store.Holds(FormEvidenceStore.General, photo.BlobRef));
        Assert.DoesNotContain("SP30", submission.AnswersJson);
        Assert.DoesNotContain("AB12CD34", submission.AnswersJson);
        Assert.Contains("Sam Smith", submission.AnswersJson);
        Assert.Equal((int)FormSubmissionStatus.Handled, submission.Status);
    }

    [Fact]
    public async Task ALicence_isCheckedOnce()
    {
        await using var forms = new PublicFormFixture();
        await VehicleFormWithAPhotoAsync(forms);
        var handler = new RecordDrivingLicenceCheckHandler(forms.Context, forms.Store);
        var command = new RecordDrivingLicenceCheck(FormId, new DateOnly(2026, 9, 23), true, "", "office@jewelps.co.uk");
        await handler.HandleAsync(command, CancellationToken.None);

        var again = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(command, CancellationToken.None));

        Assert.Equal("That licence has already been checked.", again.Message);
    }

    private static async Task<(FormSubmissionEntity Submission, FormUploadEntity Photo)> VehicleFormWithAPhotoAsync(PublicFormFixture forms)
    {
        var answers = "{\"name\":\"Sam Smith\",\"points\":\"Yes\",\"points_detail\":\"SP30 in 2025\",\"dvla_code\":\"AB12CD34\"}";
        var submission = new FormSubmissionEntity
        {
            FormSubmissionId = FormId, FormSlug = FormSlugs.CompanyVehicle, SessionId = PublicFormFixture.FirstSession,
            SubmitterName = "Sam Smith", AnswersJson = answers, SubmittedAt = DateTimeOffset.UtcNow
        };
        var photo = new FormUploadEntity
        {
            FormUploadId = "licence-photo-1", FormSubmissionId = FormId, QuestionKey = DrivingLicenceChecks.LicencePhotoKey,
            Store = (int)FormEvidenceStore.General, BlobRef = "session/licence-photo-1/licence.jpg", FileName = "licence.jpg"
        };
        forms.Context.FormSubmissions.Add(submission);
        forms.Context.FormUploads.Add(photo);
        await forms.Context.SaveChangesAsync();
        await forms.Store.SaveAsync(FormEvidenceStore.General, photo.BlobRef, "image/jpeg", new byte[] { 1, 2, 3 }, CancellationToken.None);
        return (submission, photo);
    }
}
