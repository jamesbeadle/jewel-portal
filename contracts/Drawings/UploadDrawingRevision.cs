using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Drawings;

/// <summary>
/// Uploads a new revision of a drawing. The file has already been streamed to blob storage by the
/// endpoint; this command carries the resulting <see cref="BlobRef"/> and file metadata. The new
/// revision is created <see cref="DrawingApprovalStatus.Unapproved"/> and does NOT supersede any
/// existing revision — archiving happens only on approval (see <see cref="ApproveDrawingRevision"/>).
/// The endpoint owns identifier generation so the blob path and the persisted revision share an id.
/// </summary>
public sealed record UploadDrawingRevision(
    string DrawingId,
    string DrawingRevisionId,
    string RevisionLabel,
    string FileName,
    string IssuedByEmail,
    string BlobRef,
    string ContentType,
    long FileSizeBytes) : ICommand<DrawingRevision>;
