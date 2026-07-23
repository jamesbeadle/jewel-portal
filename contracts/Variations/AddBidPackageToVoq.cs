using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Creates a bid package under a quoting variation order (linked via BidPackage.VariationOrderId).
/// The package then uses the normal procurement invite/tender flow. Tendering activity is all part
/// of the Quoting stage — the order's status does not change.
/// </summary>
public sealed record AddBidPackageToVoq(
    string VariationOrderId,
    string Title,
    string Trade,
    string OwnerEmail) : ICommand<BidPackage>;
