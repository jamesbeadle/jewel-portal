using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.BuildingControl.Attachments;

/// <summary>One file about to be stored on the case or an inspection, wherever it came from.</summary>
public sealed record BuildingControlIncomingFile(string Name, string ContentType, byte[] Content);

/// <summary>
/// The one way a file lands on a building control record: bytes into the private container, a
/// register row in the same context. Shared by the multipart upload endpoints and the
/// copy-off-the-email path so the two can never record a file differently (the
/// TenderEnquiryAttachmentWriter arrangement). Exactly one of caseId/inspectionId is set.
/// Callers save the context.
/// </summary>
public sealed class BuildingControlAttachmentWriter
{
    private const string FallbackFileName = "attachment";
    private const string FallbackContentType = "application/octet-stream";

    private readonly JpmsContext context;
    private readonly IBuildingControlAttachmentStore blobStore;

    public BuildingControlAttachmentWriter(JpmsContext context, IBuildingControlAttachmentStore blobStore)
    {
        this.context = context;
        this.blobStore = blobStore;
    }

    public async Task<BuildingControlAttachmentEntity> StoreAsync(
        string projectId, string? caseId, string? inspectionId,
        BuildingControlIncomingFile file, BuildingControlAttachmentKind kind,
        BuildingControlAttachmentSource source, string addedByEmail, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(file.Content, writable: false);
        return await StoreAsync(
            projectId, caseId, inspectionId, file.Name, file.ContentType, file.Content.LongLength,
            stream, kind, source, addedByEmail, cancellationToken);
    }

    public async Task<BuildingControlAttachmentEntity> StoreAsync(
        string projectId, string? caseId, string? inspectionId,
        string fileName, string contentType, long length, Stream content,
        BuildingControlAttachmentKind kind, BuildingControlAttachmentSource source,
        string addedByEmail, CancellationToken cancellationToken)
    {
        if ((caseId is null) == (inspectionId is null))
            throw new InvalidOperationException("A building control file belongs to exactly one of a case or an inspection.");

        var attachmentId = Guid.NewGuid().ToString("N");
        var safeFileName = string.IsNullOrWhiteSpace(fileName) ? FallbackFileName : fileName;
        var safeContentType = string.IsNullOrWhiteSpace(contentType) ? FallbackContentType : contentType;

        var blobRef = await blobStore.UploadAsync(
            projectId, caseId ?? inspectionId!, attachmentId, safeFileName, safeContentType, content, cancellationToken);

        var entity = new BuildingControlAttachmentEntity
        {
            BuildingControlAttachmentId = attachmentId,
            ProjectId = projectId,
            BuildingControlCaseId = caseId,
            BuildingControlInspectionId = inspectionId,
            Kind = (int)kind,
            FileName = safeFileName,
            ContentType = safeContentType,
            FileSizeBytes = length,
            BlobRef = blobRef,
            Source = (int)source,
            AddedAt = DateTimeOffset.UtcNow,
            AddedByEmail = addedByEmail
        };
        context.BuildingControlAttachments.Add(entity);
        return entity;
    }
}
