using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Puts a closed bid package back in play. The restored status is whatever the package's data
// implies — QuotesReceived when it holds any tender, Inviting when subcontractors were invited,
// Draft otherwise — and ClosedAt is cleared. Only a Closed package can be reopened.
public sealed record ReopenBidPackage(string BidPackageId) : ICommand<BidPackage>;
