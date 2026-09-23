using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Storage;
using Jewel.JPMS.Api.Features.Subcontractors;
using Jewel.JPMS.Api.Features.Subcontractors.Storage;
using Jewel.JPMS.Contracts.Forms;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Forms.Office.Submissions;

/// <summary>
/// A questionnaire's or insurance update's company documents onto a directory company's compliance
/// record (api/registers.js op:'formrenewal'), each with the expiry the form collected and marked as
/// having come in on a form — the renewal chase asks for those. Nothing about the person is filed
/// (FormDirectoryFilingPlan.IsFileable). An insurance update is handled once filed.
/// </summary>
public sealed class FileFormToDirectoryHandler : ICommandHandler<FileFormToDirectory, FormDirectoryFiling>
{
    private readonly JpmsContext context;
    private readonly IFormEvidenceStore formStore;
    private readonly IComplianceBlobStore complianceStore;
    private readonly ICommandHandler<AddComplianceDocumentVersion, ComplianceDocument> addVersion;

    public FileFormToDirectoryHandler(
        JpmsContext context, IFormEvidenceStore formStore, IComplianceBlobStore complianceStore,
        ICommandHandler<AddComplianceDocumentVersion, ComplianceDocument> addVersion)
    {
        this.context = context;
        this.formStore = formStore;
        this.complianceStore = complianceStore;
        this.addVersion = addVersion;
    }

    public async Task<FormDirectoryFiling> HandleAsync(FileFormToDirectory command, CancellationToken cancellationToken)
    {
        var submission = await context.FormSubmissions.FirstOrDefaultAsync(row => row.FormSubmissionId == command.FormSubmissionId, cancellationToken)
            ?? throw new InvalidOperationException("That form no longer exists.");
        var carriesCertificates = submission.FormSlug is FormSlugs.SubcontractorQuestionnaire or FormSlugs.InsuranceUpdate;
        if (!carriesCertificates) throw new InvalidOperationException("Only a questionnaire or an insurance update carries certificates.");
        var company = await context.Subcontractors.AsNoTracking()
            .FirstOrDefaultAsync(row => row.SubcontractorId == command.SubcontractorId, cancellationToken)
            ?? throw new InvalidOperationException("That directory record no longer exists.");
        var filed = new List<string>();
        foreach (var file in command.Files) filed.Add(await FileOneAsync(submission, company.SubcontractorId, file, cancellationToken));
        var isAnInsuranceUpdate = submission.FormSlug == FormSlugs.InsuranceUpdate;
        if (isAnInsuranceUpdate) MarkHandled(submission, command.FiledByEmail);
        await context.SaveChangesAsync(cancellationToken);
        return new FormDirectoryFiling(submission.FormSubmissionId, company.SubcontractorId, company.CompanyName, filed);
    }

    private async Task<string> FileOneAsync(
        FormSubmissionEntity submission, string subcontractorId, FormUploadToFile file, CancellationToken cancellationToken)
    {
        var upload = await context.FormUploads.AsNoTracking().FirstOrDefaultAsync(
            row => row.FormUploadId == file.FormUploadId && row.FormSubmissionId == submission.FormSubmissionId && row.DeletedAt == null,
            cancellationToken) ?? throw new InvalidOperationException("That file is not on this form.");
        var form = FormCatalogue.For(submission.FormSlug)!;
        var isACompanyDocument = FormDirectoryFilingPlan.IsFileable(form, upload.QuestionKey);
        if (!isACompanyDocument) throw new InvalidOperationException($"{upload.FileName} is about the person, not the company, and is never filed.");
        var blob = await formStore.OpenAsync((FormEvidenceStore)upload.Store, upload.BlobRef, cancellationToken)
            ?? throw new InvalidOperationException($"{upload.FileName} is missing from storage.");
        var documentId = SubcontractorIdentifierFactory.NextComplianceDocumentId();
        await using var content = blob.Content;
        var blobPath = await complianceStore.UploadAsync(subcontractorId, documentId, upload.FileName, upload.ContentType, content, cancellationToken);
        var kind = file.Kind.Trim();
        var version = new AddComplianceDocumentVersion(documentId, subcontractorId, kind, upload.FileName, FormDates.AsMidnight(file.ExpiresOn), blobPath,
            upload.ContentType, upload.Size, file.PublicLiabilityCover, IsFromAForm: true);
        await addVersion.HandleAsync(version, cancellationToken);
        return kind;
    }

    private static void MarkHandled(FormSubmissionEntity submission, string filedByEmail)
    {
        submission.Status = (int)FormSubmissionStatus.Handled;
        submission.HandledByEmail = filedByEmail;
        submission.HandledAt = DateTimeOffset.UtcNow;
    }
}

public sealed class FileFormToDirectoryAuthorisation
{
    public bool Allows(SignedInUser user, FileFormToDirectory command) => FormRoleSets.Office.IncludesAny(user.Roles);
}

public sealed class FileFormToDirectoryValidation
{
    private const int LongestKind = 128;

    public ValidationOutcome Check(FileFormToDirectory command)
    {
        var errors = new List<string>();
        var files = command.Files ?? Array.Empty<FormUploadToFile>();
        var isEveryFileNamed = files.All(file => file.Kind?.Trim().Length is > 0 and <= LongestKind);
        if (string.IsNullOrWhiteSpace(command.FormSubmissionId)) errors.Add("Which form?");
        if (string.IsNullOrWhiteSpace(command.SubcontractorId)) errors.Add("Pick the directory company to file it to.");
        if (files.Count == 0) errors.Add("Pick at least one file to file.");
        if (!isEveryFileNamed) errors.Add($"Say what each file is, in at most {LongestKind} characters.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
