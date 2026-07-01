using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

/// <summary>
/// Persists a newly uploaded revision as <see cref="DrawingApprovalStatus.Unapproved"/>. It does NOT
/// supersede or touch any sibling revision — that happens only when a revision is approved.
/// </summary>
public sealed class UploadDrawingRevisionHandler
    : ICommandHandler<UploadDrawingRevision, DrawingRevision>
{
    private readonly JpmsContext context;

    public UploadDrawingRevisionHandler(JpmsContext context) { this.context = context; }

    public async Task<DrawingRevision> HandleAsync(UploadDrawingRevision command, CancellationToken cancellationToken)
    {
        var drawing = await context.Drawings.FindAsync(new object[] { command.DrawingId }, cancellationToken);
        if (drawing is null) throw new InvalidOperationException($"Drawing {command.DrawingId} not found.");

        var revision = new DrawingRevisionEntity
        {
            DrawingRevisionId = command.DrawingRevisionId,
            DrawingId = command.DrawingId,
            RevisionLabel = command.RevisionLabel,
            FileName = command.FileName,
            IssuedByEmail = command.IssuedByEmail,
            ReceivedAt = DateTimeOffset.UtcNow,
            SupersededAt = null,
            IsAmbiguous = string.IsNullOrWhiteSpace(command.RevisionLabel) || command.RevisionLabel == "?",
            ViewCount = 0,
            ApprovalStatus = (int)DrawingApprovalStatus.Unapproved,
            BlobRef = command.BlobRef,
            ContentType = command.ContentType,
            FileSizeBytes = command.FileSizeBytes
        };

        context.DrawingRevisions.Add(revision);
        await context.SaveChangesAsync(cancellationToken);
        return revision.ToModel();
    }
}
