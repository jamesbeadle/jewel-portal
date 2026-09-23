using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Public;

public sealed partial class PublicFormService
{
    /// <summary>
    /// One file, stored before the form is sent, so a big photo never holds the answers hostage and a
    /// dropped signal loses one file, not the form. It lands in the form's own store; until the form
    /// is sent it belongs to nobody, which is what marks an abandoned upload for the retention sweep.
    /// </summary>
    public async Task<PublicFormUploadReceipt> UploadAsync(
        string slug, PublicFormUpload upload, string clientHash, CancellationToken cancellationToken)
    {
        var form = Known(slug);
        var question = form.QuestionFor(upload.QuestionKey);
        var takesAFile = question is { Kind: FormQuestionKind.Upload or FormQuestionKind.Signature };
        if (!takesAFile) throw new PublicFormRefusal("That question does not take a file.");
        CheckSession(upload.SessionId);
        var bytes = PublicFormFiles.Decoded(upload.Base64);
        await CheckUploadLimitsAsync(upload.SessionId, clientHash, cancellationToken);
        var entity = NewUpload(form, upload, clientHash, bytes.LongLength);
        await store.SaveAsync(form.Store, entity.BlobRef, entity.ContentType, bytes, cancellationToken);
        context.FormUploads.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new PublicFormUploadReceipt(entity.FormUploadId, entity.FileName);
    }

    private static FormUploadEntity NewUpload(FormDefinition form, PublicFormUpload upload, string clientHash, long size)
    {
        var formUploadId = FormIdentifierFactory.NextId();
        var fileName = PublicFormFiles.SafeName(upload.FileName);
        return new FormUploadEntity
        {
            FormUploadId = formUploadId,
            SessionId = upload.SessionId,
            FormSlug = form.Slug,
            QuestionKey = upload.QuestionKey,
            Store = (int)form.Store,
            BlobRef = $"{upload.SessionId}/{formUploadId}/{fileName}",
            FileName = fileName,
            ContentType = PublicFormFiles.ContentTypeOf(fileName),
            Size = size,
            UploadedAt = DateTimeOffset.UtcNow,
            ClientHash = clientHash
        };
    }
}
