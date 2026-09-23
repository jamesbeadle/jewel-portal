using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Mapping;
using Jewel.JPMS.Api.Features.Forms.Public;
using Jewel.JPMS.Api.Features.Forms.Storage;

namespace Jewel.JPMS.Api.Features.Forms.Registers;

/// <summary>
/// The checker's evidence — a photo of the document page, or the online check's result — filed in the
/// restricted right-to-work store against the check (api/training-cert.js type 'rtw'). An earlier
/// file stays on record: evidence is kept for the engagement plus two years, then the sweep takes it all.
/// </summary>
public sealed class RightToWorkEvidenceFiling
{
    public const string EvidenceKey = "evidence";
    private const long LargestEvidenceBytes = 25L * 1024 * 1024;
    private readonly JpmsContext context;
    private readonly IFormEvidenceStore store;

    public RightToWorkEvidenceFiling(JpmsContext context, IFormEvidenceStore store)
    {
        this.context = context;
        this.store = store;
    }

    public async Task<RightToWorkCheck> FileAsync(string rightToWorkCheckId, IFormFile file, CancellationToken cancellationToken)
    {
        var check = await context.RightToWorkChecks.FirstOrDefaultAsync(row => row.RightToWorkCheckId == rightToWorkCheckId, cancellationToken)
            ?? throw new InvalidOperationException("That check no longer exists.");
        var isTooBig = file.Length == 0 || file.Length > LargestEvidenceBytes;
        if (isTooBig) throw new InvalidOperationException("The evidence file must be under 25 MB.");
        var upload = NewEvidence(check, PublicFormFiles.SafeName(file.FileName), file.Length);
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        await store.SaveAsync(FormEvidenceStore.RightToWork, upload.BlobRef, upload.ContentType, buffer.ToArray(), cancellationToken);
        context.FormUploads.Add(upload);
        check.EvidenceUploadId = upload.FormUploadId;
        check.IsEvidenceFiled = true;
        await context.SaveChangesAsync(cancellationToken);
        return check.ToModel(upload.FileName);
    }

    private static FormUploadEntity NewEvidence(RightToWorkCheckEntity check, string fileName, long size)
    {
        var formUploadId = FormIdentifierFactory.NextId();
        return new FormUploadEntity
        {
            FormUploadId = formUploadId,
            SessionId = FormUploadFolders.OfficeSession,
            FormSlug = FormSlugs.RightToWork,
            Company = check.Company,
            QuestionKey = EvidenceKey,
            Store = (int)FormEvidenceStore.RightToWork,
            BlobRef = $"{FormUploadFolders.RightToWorkEvidence(check.RightToWorkCheckId)}{formUploadId}/{fileName}",
            FileName = fileName,
            ContentType = PublicFormFiles.ContentTypeOf(fileName),
            Size = size,
            UploadedAt = DateTimeOffset.UtcNow
        };
    }
}
