using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Procurement;

/// <summary>
/// Deletes a bid package outright — for packages raised in error or AI suggestions that turned
/// out not to be wanted. Cascades everything that only exists under the package: invited
/// subcontractors (the invite rows, not the directory entries), line items, quotes and their
/// lines, tender-document attachments (blobs included) and drawing links. Emails tagged
/// "JPMS/BPI-…" stay in the mailbox — they are correspondence, not package data.
///
/// Refused for an AWARDED package, and while any work order references it — the order carries
/// the committed money, so cancel or reject it first. Closing remains the polite no-winner
/// ending for a real tender; deletion is for records that should never have existed.
/// </summary>
public sealed record DeleteBidPackage(string BidPackageId) : ICommand<Acknowledgement>;
