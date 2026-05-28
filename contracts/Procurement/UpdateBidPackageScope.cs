using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

public sealed record UpdateBidPackageScope(
    string BidPackageId,
    string Title,
    string Trade,
    BidPackageStatus Status,
    string OwnerEmail) : ICommand<BidPackage>;
