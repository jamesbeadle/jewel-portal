using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// MaterialsApplicable marks scopes where materials matter: the tender invite then asks each
// subcontractor to state whether they will supply their own materials or price labour-only.
public sealed record CreateBidPackage(
    string ProjectId,
    string Title,
    string Trade,
    string OwnerEmail,
    bool MaterialsApplicable = false) : ICommand<BidPackage>;
