using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Files kept on a bid package as tender documents. One file, because the whole feature is two
// messages over one table — mirroring WorkOrderAttachmentContracts. Unlike work-order attachments
// these ARE supplier-facing: PrepareBidPackageInviteDraft attaches them to the invite email
// alongside the linked drawings.

/// <summary>Everything attached to a bid package, oldest first — the order it was added in.</summary>
public sealed record ListBidPackageAttachments(string BidPackageId)
    : IQuery<IReadOnlyList<BidPackageAttachment>>;

/// <summary>Removes one attachment (and its stored file). An ordinary tidy-up before the invite
/// goes out, not a business event.</summary>
public sealed record RemoveBidPackageAttachment(
    string BidPackageId,
    string BidPackageAttachmentId) : ICommand<IReadOnlyList<BidPackageAttachment>>;

// Uploading a file is multipart/form-data and is posted directly by the client store rather than
// through the JSON command sender — the same arrangement work-order and request attachments use.
// See POST /api/bid-packages/{bidPackageId}/attachments.
