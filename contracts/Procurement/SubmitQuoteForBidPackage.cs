using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

public sealed record SubmitQuoteForBidPackage(
    string BidPackageId,
    string SubcontractorId,
    decimal Value,
    string Notes) : ICommand<Quote>;
