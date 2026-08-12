using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Ends the tender process without picking a winner — all tenderers declined, the works were
// re-scoped, or the package simply lapsed. No reason is recorded: a process that can close without
// incident closes without paperwork. Stamps ClosedAt; an Awarded package can't be closed (the
// award already ended it). ReopenBidPackage undoes this.
public sealed record CloseBidPackage(string BidPackageId) : ICommand<BidPackage>;
