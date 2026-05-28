using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

public sealed record AwardBidPackage(
    string BidPackageId,
    string ProjectId,
    string SubcontractorId,
    decimal Value,
    string Scope,
    string AwardedByEmail) : ICommand<WorkOrder>;
