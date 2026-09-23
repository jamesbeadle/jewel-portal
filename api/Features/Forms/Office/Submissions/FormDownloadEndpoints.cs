using Jewel.JPMS.Api.Features.Forms.Documents;
using Jewel.JPMS.Api.Features.Forms.Storage;
using Jewel.JPMS.Api.Storage;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Office.Submissions;

/// <summary>
/// A form's file, streamed through the API so no store is ever public, and the form as a PDF record.
/// Both open only for the readers of the store the form lives in. Download endpoints render inline
/// with no separate handler, as every other download in the api does.
/// </summary>
public sealed class FormDownloadEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly FormSubmissionAccess access;
    private readonly IFormEvidenceStore store;
    private readonly IQueryHandler<OpenFormSubmission, FormSubmissionView> get;

    public FormDownloadEndpoints(
        SignedInUserResolver users, FormSubmissionAccess access, IFormEvidenceStore store,
        IQueryHandler<OpenFormSubmission, FormSubmissionView> get)
    {
        this.users = users;
        this.access = access;
        this.store = store;
        this.get = get;
    }

    [Function("DownloadFormUpload")]
    public async Task<IActionResult> File(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "form-uploads/{formUploadId}/file")] HttpRequest request,
        string formUploadId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!FormRoleSets.AnyReader.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        var upload = await access.ReadableUploadAsync(signedInUser, formUploadId, cancellationToken);
        if (upload is null) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        if (upload.DeletedAt is not null) return new NotFoundObjectResult($"That file was deleted: {upload.DeletionReason}");
        var blob = await store.OpenAsync((FormEvidenceStore)upload.Store, upload.BlobRef, cancellationToken);
        if (blob is null) return new NotFoundObjectResult("The stored file could not be found.");
        InlineRendering.ForbidSniffing(request.HttpContext.Response);
        var result = new FileStreamResult(blob.Content, upload.ContentType) { EnableRangeProcessing = true };
        var isInline = InlineRendering.IsInlineView(InlineRendering.IsAskedFor(request), upload.ContentType);
        if (!isInline) result.FileDownloadName = upload.FileName;
        return result;
    }

    [Function("DownloadFormRecordPdf")]
    public async Task<IActionResult> Pdf(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "form-submissions/{formSubmissionId}/pdf")] HttpRequest request,
        string formSubmissionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var mayRead = FormRoleSets.AnyReader.IncludesAny(signedInUser.Roles) && await access.MayReadAsync(signedInUser, formSubmissionId, cancellationToken);
        if (!mayRead) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        var view = await get.HandleAsync(new OpenFormSubmission(formSubmissionId), cancellationToken);
        var pdf = FormRecordPdf.Render(view, DateTimeOffset.UtcNow);
        return new FileContentResult(pdf, "application/pdf") { FileDownloadName = FileNameOf(view.Submission) };
    }

    private static string FileNameOf(FormSubmission submission)
    {
        var title = FormCatalogue.TitleOf(submission.FormSlug);
        var safe = string.Concat($"{title} - {submission.SubmitterName}".Where(character => !Path.GetInvalidFileNameChars().Contains(character)));
        return $"{safe} {submission.SubmittedAt:yyyy-MM-dd}.pdf";
    }
}
