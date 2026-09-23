using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Storage;

namespace Jewel.JPMS.Api.Features.Forms.Office.Submissions;

/// <summary>
/// Whether a signed-in person may open a form or one of its files. Everyone in the office sees that a
/// form came in; its answers and files open only for the readers of the store it lives in — the
/// right-to-work and payroll-starter stores are narrower than the office. A file nobody sent a form
/// with is abandoned and opens for nobody; the office's own right-to-work evidence is the exception.
/// </summary>
public sealed class FormSubmissionAccess
{
    private readonly JpmsContext context;

    public FormSubmissionAccess(JpmsContext context)
    {
        this.context = context;
    }

    public Task<bool> MayReadAsync(SignedInUser user, string formSubmissionId, CancellationToken cancellationToken) =>
        FormRecordScope.MayActOnFormAsync(context, user, formSubmissionId, cancellationToken);

    public async Task<FormUploadEntity?> ReadableUploadAsync(SignedInUser user, string formUploadId, CancellationToken cancellationToken)
    {
        var upload = await context.FormUploads.AsNoTracking().FirstOrDefaultAsync(row => row.FormUploadId == formUploadId, cancellationToken);
        if (upload is null) return null;
        var isOnRecord = upload.FormSubmissionId is not null || upload.SessionId == FormUploadFolders.OfficeSession;
        var readers = FormRoleSets.ReadersOf((FormEvidenceStore)upload.Store);
        return isOnRecord && readers.IncludesAny(user.Roles) ? upload : null;
    }
}
