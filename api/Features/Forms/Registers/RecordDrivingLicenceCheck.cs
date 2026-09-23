using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Answers;
using Jewel.JPMS.Api.Features.Forms.Mapping;
using Jewel.JPMS.Api.Features.Forms.Storage;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Registers;

/// <summary>
/// The office's licence check on a Company Vehicle Form, and the promise the form's own privacy
/// paragraph makes, kept: the licence photograph is deleted once verified, and only whether the
/// person meets the insurance criteria is recorded — the driving-record answers and the DVLA check
/// code are withheld from the stored form for good. Both are real jobs done here, not sentences.
/// </summary>
public sealed class RecordDrivingLicenceCheckHandler : ICommandHandler<RecordDrivingLicenceCheck, DrivingLicenceCheck>
{
    private const string PhotoDeleted = "Licence photograph deleted once verified, as the form's privacy notice promises.";
    private readonly JpmsContext context;
    private readonly IFormEvidenceStore store;

    public RecordDrivingLicenceCheckHandler(JpmsContext context, IFormEvidenceStore store)
    {
        this.context = context;
        this.store = store;
    }

    public async Task<DrivingLicenceCheck> HandleAsync(RecordDrivingLicenceCheck command, CancellationToken cancellationToken)
    {
        var submission = await context.FormSubmissions.FirstOrDefaultAsync(row => row.FormSubmissionId == command.FormSubmissionId, cancellationToken)
            ?? throw new InvalidOperationException("That form no longer exists.");
        var isAVehicleForm = submission.FormSlug == FormSlugs.CompanyVehicle && submission.DestroyedAt is null;
        if (!isAVehicleForm) throw new InvalidOperationException("Only a Company Vehicle Form carries a licence check.");
        var alreadyChecked = await context.DrivingLicenceChecks.AnyAsync(row => row.FormSubmissionId == submission.FormSubmissionId, cancellationToken);
        if (alreadyChecked) throw new InvalidOperationException("That licence has already been checked.");
        var now = DateTimeOffset.UtcNow;
        var photosDeleted = await DeleteLicencePhotosAsync(submission.FormSubmissionId, now, cancellationToken);
        WithholdTheDrivingRecord(submission, now);
        var check = NewCheck(command, photosDeleted, now);
        context.DrivingLicenceChecks.Add(check);
        submission.Status = (int)FormSubmissionStatus.Handled;
        submission.HandledByEmail = command.CheckedByEmail;
        submission.HandledAt = now;
        await context.SaveChangesAsync(cancellationToken);
        return check.ToModel();
    }

    private async Task<int> DeleteLicencePhotosAsync(string formSubmissionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var photos = await context.FormUploads
            .Where(row => row.FormSubmissionId == formSubmissionId && row.QuestionKey == DrivingLicenceChecks.LicencePhotoKey && row.DeletedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var photo in photos)
        {
            await store.DeleteAsync((FormEvidenceStore)photo.Store, photo.BlobRef, cancellationToken);
            photo.DeletedAt = now;
            photo.DeletionReason = PhotoDeleted;
        }
        return photos.Count;
    }

    private static void WithholdTheDrivingRecord(FormSubmissionEntity submission, DateTimeOffset now)
    {
        var answers = FormAnswersJson.Read(submission.AnswersJson);
        foreach (var key in DrivingLicenceChecks.AnswersWithheldAfterTheCheck.Where(answers.ContainsKey))
            answers[key] = DrivingLicenceChecks.Withheld;
        submission.AnswersJson = FormAnswersJson.Write(answers);
        submission.RedactedAt = now;
    }

    private static DrivingLicenceCheckEntity NewCheck(RecordDrivingLicenceCheck command, int photosDeleted, DateTimeOffset now) => new()
    {
        DrivingLicenceCheckId = FormIdentifierFactory.NextId(),
        FormSubmissionId = command.FormSubmissionId,
        DvlaCheckedOn = command.DvlaCheckedOn,
        IsWithinInsuranceCriteria = command.IsWithinInsuranceCriteria,
        Note = (command.Note ?? "").Trim(),
        CheckedByEmail = command.CheckedByEmail,
        CheckedAt = now,
        PhotosDeleted = photosDeleted
    };
}

public sealed class RecordDrivingLicenceCheckAuthorisation
{
    public bool Allows(SignedInUser user, RecordDrivingLicenceCheck command) => FormRoleSets.Office.IncludesAny(user.Roles);
}

public sealed class RecordDrivingLicenceCheckValidation
{
    public ValidationOutcome Check(RecordDrivingLicenceCheck command)
    {
        var errors = new List<string>();
        var isInTheFuture = command.DvlaCheckedOn > FormClock.Today();
        if (string.IsNullOrWhiteSpace(command.FormSubmissionId)) errors.Add("Which form?");
        if (isInTheFuture) errors.Add("The DVLA check cannot be dated in the future.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
