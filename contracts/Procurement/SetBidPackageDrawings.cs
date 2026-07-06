using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Replace the set of project drawings linked to a bid package (the tender documents the invite
// email attaches). Wholesale replacement, mirroring SetBidPackageLineItems: the UI sends the full
// desired set. Returns the linked drawings, newest first.
public sealed record SetBidPackageDrawings(
    string BidPackageId,
    IReadOnlyList<string> DrawingIds) : ICommand<IReadOnlyList<Drawing>>;
