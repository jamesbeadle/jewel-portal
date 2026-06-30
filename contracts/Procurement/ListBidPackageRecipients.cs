using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// The subcontractors invited to tender for a bid package, with their invite status and timestamps.
public sealed record ListBidPackageRecipients(string BidPackageId) : IQuery<IReadOnlyList<BidPackageRecipient>>;
