using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Award the package to a subcontractor, creating the work order (the purchase-order record the
// commercial team raises against). Marks the winning recipient Won, moves the package to Awarded.
// QuoteId (optional) records which submission won when awarding from the quote comparison.
public sealed record AwardBidPackage(
    string BidPackageId,
    string ProjectId,
    string SubcontractorId,
    decimal Value,
    string Scope,
    string AwardedByEmail,
    string? QuoteId = null) : ICommand<WorkOrder>;
