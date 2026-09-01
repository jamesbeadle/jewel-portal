using System.IO.Compression;
using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

// Builds the queue item a zip entry lands as: the parent's email envelope and project hint carry
// over (the child still came from that email), the file facts come from the entry, and
// SourceDocumentControlItemId records the provenance. The blob ref is set by the handler once the
// bytes are copied out.
public static class ExtractedArchiveChildFactory
{
    private const int FileNameMaximumLength = 256;

    public static DocumentControlItemEntity Build(
        DocumentControlItemEntity parent, ZipArchiveEntry entry, string childAttachmentId, string sentBy) =>
        new()
        {
            DocumentControlItemId = DocumentControlIdentifierFactory.NextDocumentControlItemId(),
            MessageId = parent.MessageId,
            InternetMessageId = parent.InternetMessageId,
            AttachmentId = childAttachmentId,
            FromEmail = parent.FromEmail,
            FromName = parent.FromName,
            Subject = parent.Subject,
            ReceivedAt = parent.ReceivedAt,
            FileName = FitFileName(entry.Name),
            ContentType = ArchiveEntryScreen.ContentTypeFor(entry.Name),
            FileSizeBytes = entry.Length,
            ProjectIdHint = parent.ProjectIdHint,
            Status = (int)DocumentControlStatus.Pending,
            SentBy = sentBy,
            SentAt = DateTimeOffset.UtcNow,
            SourceDocumentControlItemId = parent.DocumentControlItemId
        };

    // Zip entry names can legitimately overrun the column — trim from the stem, never the
    // extension, so the preview and content-type mapping keep working.
    private static string FitFileName(string name)
    {
        if (name.Length <= FileNameMaximumLength) return name;
        var extension = Path.GetExtension(name);
        var stem = Path.GetFileNameWithoutExtension(name);
        return stem[..(FileNameMaximumLength - extension.Length)] + extension;
    }
}
