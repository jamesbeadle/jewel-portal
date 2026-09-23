using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Answers;
using Jewel.JPMS.Api.Features.Forms.Mapping;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Registers;

/// <summary>
/// A Training Certificate form onto the training register (api/registers.js op:'formtraining'): the
/// office picks the person and the course, the record is made or brought up to date with the dates,
/// the certificate is the form's first file, and the form is handled. The value is the expiry date —
/// the register chases it; a certificate with none is never chased and never expires.
/// </summary>
public sealed class AcceptTrainingCertificateHandler : ICommandHandler<AcceptTrainingCertificate, TrainingRecord>
{
    private readonly JpmsContext context;

    public AcceptTrainingCertificateHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<TrainingRecord> HandleAsync(AcceptTrainingCertificate command, CancellationToken cancellationToken)
    {
        var submission = await context.FormSubmissions.FirstOrDefaultAsync(row => row.FormSubmissionId == command.FormSubmissionId, cancellationToken)
            ?? throw new InvalidOperationException("That form no longer exists.");
        var isATrainingForm = submission.FormSlug == FormSlugs.TrainingCertificate;
        if (!isATrainingForm) throw new InvalidOperationException("Only a Training Certificate form can go onto the training register.");
        var record = await RecordToWriteAsync(command, cancellationToken);
        var answers = FormAnswersJson.Read(submission.AnswersJson);
        var certificate = await context.FormUploads.AsNoTracking()
            .Where(row => row.FormSubmissionId == submission.FormSubmissionId && row.DeletedAt == null)
            .OrderBy(row => row.UploadedAt).FirstOrDefaultAsync(cancellationToken);
        TrainingRecordWriting.Accept(record, command, submission, answers, certificate?.FormUploadId);
        submission.Status = (int)FormSubmissionStatus.Handled;
        submission.HandledByEmail = command.AcceptedByEmail;
        submission.HandledAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return record.ToModel();
    }

    private async Task<TrainingRecordEntity> RecordToWriteAsync(AcceptTrainingCertificate command, CancellationToken cancellationToken)
    {
        var person = command.PersonName.Trim().ToLower();
        var course = command.Course.Trim().ToLower();
        var existing = await context.TrainingRecords.FirstOrDefaultAsync(
            row => row.PersonName.ToLower() == person && row.Course.ToLower() == course, cancellationToken);
        if (existing is not null) return existing;
        var created = new TrainingRecordEntity { TrainingRecordId = FormIdentifierFactory.NextId() };
        context.TrainingRecords.Add(created);
        return created;
    }
}

public sealed class AcceptTrainingCertificateAuthorisation
{
    public bool Allows(SignedInUser user, AcceptTrainingCertificate command) => FormRoleSets.Office.IncludesAny(user.Roles);
}

public sealed class AcceptTrainingCertificateValidation
{
    public ValidationOutcome Check(AcceptTrainingCertificate command)
    {
        var errors = new List<string>();
        var expiresBeforeCompleted = command.ExpiresOn is { } expiry && expiry < command.CompletedOn;
        if (string.IsNullOrWhiteSpace(command.FormSubmissionId)) errors.Add("Which form?");
        if (string.IsNullOrWhiteSpace(command.PersonName)) errors.Add("Whose certificate is it?");
        if (string.IsNullOrWhiteSpace(command.Course)) errors.Add("Which course or ticket is it?");
        if (expiresBeforeCompleted) errors.Add("The expiry is before the completed date.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
