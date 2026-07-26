using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// Attachments on a request: linked drawing revisions and uploaded files (site photos). One file,
// because the whole feature is three messages over one table.

/// <summary>Everything attached to a request, oldest first — the order it was added in.</summary>
public sealed record ListRequestAttachments(string RequestId)
    : IQuery<IReadOnlyList<RequestAttachment>>;

/// <summary>
/// Links drawing revisions from the project's register to a request. Linking the same revision
/// twice is a no-op, so a user re-picking from the drawing list cannot create duplicates.
/// </summary>
public sealed record AttachDrawingsToRequest(
    string RequestId,
    IReadOnlyList<string> DrawingRevisionIds) : ICommand<IReadOnlyList<RequestAttachment>>;

/// <summary>Removes one attachment. A linked drawing is only unlinked; its revision is untouched.</summary>
public sealed record RemoveRequestAttachment(
    string RequestId,
    string RequestAttachmentId) : ICommand<IReadOnlyList<RequestAttachment>>;

// Uploading a file is multipart/form-data and is posted directly by the client store rather than
// through the JSON command sender — the same arrangement drawing revisions use. See
// POST /api/requests/{requestId}/attachments.
