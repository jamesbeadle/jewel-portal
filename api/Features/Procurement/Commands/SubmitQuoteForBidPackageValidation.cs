using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SubmitQuoteForBidPackageValidation
{
    public ValidationOutcome Check(SubmitQuoteForBidPackage command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.BidPackageId)) errors.Add("BidPackageId is required.");
        if (string.IsNullOrWhiteSpace(command.SubcontractorId)) errors.Add("SubcontractorId is required.");
        if (command.Value <= 0) errors.Add("Quote value must be positive.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
