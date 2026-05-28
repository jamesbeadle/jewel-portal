using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

public sealed record CreateBidPackage(
    string ProjectId,
    string Title,
    string Trade,
    string OwnerEmail) : ICommand<BidPackage>;
