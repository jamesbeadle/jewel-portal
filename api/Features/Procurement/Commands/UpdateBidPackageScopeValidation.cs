using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class UpdateBidPackageScopeValidation
{
    public ValidationOutcome Check(UpdateBidPackageScope command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.BidPackageId)) errors.Add("BidPackageId is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("Title is required.");
        if (string.IsNullOrWhiteSpace(command.Trade)) errors.Add("Trade is required.");
        if (string.IsNullOrWhiteSpace(command.OwnerEmail)) errors.Add("Owner email is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
