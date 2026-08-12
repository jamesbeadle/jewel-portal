using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

public sealed record UpdateBidPackageScope(
    string BidPackageId,
    string Title,
    string Trade,
    BidPackageStatus Status,
    string OwnerEmail,
    bool MaterialsApplicable = false,
    // Null means "leave unchanged" — a caller that doesn't carry the field can never blank it.
    string? SpecificationSummary = null) : ICommand<BidPackage>;
