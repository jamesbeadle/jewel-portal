using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Attachments kept on a work order for record keeping. One file, because the whole feature is
// two messages over one table — mirroring RequestAttachmentContracts. Attachments never reach
// the supplier: the purchase-order email and the printed PO ignore them entirely.

/// <summary>Everything attached to a work order, oldest first — the order it was added in.</summary>
public sealed record ListWorkOrderAttachments(string WorkOrderId)
    : IQuery<IReadOnlyList<WorkOrderAttachment>>;

/// <summary>Removes one attachment (and its stored file). Record keeping only, so removal is
/// an ordinary tidy-up, not a business event.</summary>
public sealed record RemoveWorkOrderAttachment(
    string WorkOrderId,
    string WorkOrderAttachmentId) : ICommand<IReadOnlyList<WorkOrderAttachment>>;

// Uploading a file is multipart/form-data and is posted directly by the client store rather than
// through the JSON command sender — the same arrangement request attachments use. See
// POST /api/work-orders/{workOrderId}/attachments. Email attachments picked during triage travel
// as AttachmentIds on CreateWorkOrderFromMessage and are copied server-side, so the bytes never
// round-trip through the browser.
