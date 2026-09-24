using Jewel.JPMS.Api.Features.Forms.Documents;
using Jewel.JPMS.Api.Features.Forms.Quizzes;
using Jewel.JPMS.Api.Features.Subcontractors;
using Jewel.JPMS.Api.Features.Subcontractors.Storage;
using Jewel.JPMS.Contracts.Forms;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Forms.Office.Submissions;

/// <summary>
/// A marked quiz onto a directory company's compliance record, as due diligence evidence: the quiz's
/// record (the same PDF the office downloads, its score in the header) is stored as the company's
/// current quiz, named with the result and dated by QuizComplianceRecord. The quiz is then handled.
/// </summary>
public sealed class FileQuizToDirectoryHandler : ICommandHandler<FileQuizToDirectory, FormDirectoryFiling>
{
    private const string PdfContentType = "application/pdf";

    private readonly JpmsContext context;
    private readonly IComplianceBlobStore complianceStore;
    private readonly ICommandHandler<AddComplianceDocumentVersion, ComplianceDocument> addVersion;

    public FileQuizToDirectoryHandler(
        JpmsContext context, IComplianceBlobStore complianceStore,
        ICommandHandler<AddComplianceDocumentVersion, ComplianceDocument> addVersion)
    {
        this.context = context;
        this.complianceStore = complianceStore;
        this.addVersion = addVersion;
    }

    public async Task<FormDirectoryFiling> HandleAsync(FileQuizToDirectory command, CancellationToken cancellationToken)
    {
        var submission = await context.FormSubmissions.FirstOrDefaultAsync(row => row.FormSubmissionId == command.FormSubmissionId, cancellationToken)
            ?? throw new InvalidOperationException("That form no longer exists.");
        var uploads = await context.FormUploads.AsNoTracking()
            .Where(row => row.FormSubmissionId == command.FormSubmissionId).ToListAsync(cancellationToken);
        var view = FormSubmissionReading.ViewOf(submission, uploads);
        var score = view.QuizScore ?? throw new InvalidOperationException("Only a quiz is marked and filed this way.");
        var company = await context.Subcontractors.AsNoTracking()
            .FirstOrDefaultAsync(row => row.SubcontractorId == command.SubcontractorId, cancellationToken)
            ?? throw new InvalidOperationException("That directory record no longer exists.");
        await FileRecordAsync(view, score, company.SubcontractorId, cancellationToken);
        submission.Status = (int)FormSubmissionStatus.Handled;
        submission.HandledByEmail = command.FiledByEmail;
        submission.HandledAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return new FormDirectoryFiling(submission.FormSubmissionId, company.SubcontractorId, company.CompanyName, new[] { QuizComplianceRecord.Kind });
    }

    private async Task FileRecordAsync(FormSubmissionView view, FormQuizScore score, string subcontractorId, CancellationToken cancellationToken)
    {
        var record = FormRecordPdf.Render(view, DateTimeOffset.UtcNow);
        var fileName = QuizComplianceRecord.FileNameFor(score);
        var documentId = SubcontractorIdentifierFactory.NextComplianceDocumentId();
        using var content = new MemoryStream(record);
        var blobPath = await complianceStore.UploadAsync(subcontractorId, documentId, fileName, PdfContentType, content, cancellationToken);
        var expiresOn = QuizComplianceRecord.ExpiresOn(score, view.Submission.SubmittedAt);
        var version = new AddComplianceDocumentVersion(documentId, subcontractorId, QuizComplianceRecord.Kind, fileName,
            FormDates.AsMidnight(expiresOn), blobPath, PdfContentType, record.Length, IsFromAForm: true);
        await addVersion.HandleAsync(version, cancellationToken);
    }
}

public sealed class FileQuizToDirectoryAuthorisation
{
    public bool Allows(SignedInUser user, FileQuizToDirectory command) => FormRoleSets.Office.IncludesAny(user.Roles);
}

public sealed class FileQuizToDirectoryValidation
{
    public ValidationOutcome Check(FileQuizToDirectory command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.FormSubmissionId)) errors.Add("Which quiz?");
        if (string.IsNullOrWhiteSpace(command.SubcontractorId)) errors.Add("Pick the directory company to file it to.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
