using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SetBidPackageLineItemCoverageValidation
{
    public ValidationOutcome Check(SetBidPackageLineItemCoverage command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.LineItemId)) errors.Add("LineItemId is required.");

        switch (command.Coverage)
        {
            case BidPackageLineCoverage.ContractLine:
                if (string.IsNullOrWhiteSpace(command.BoqLineItemId)) errors.Add("BoqLineItemId is required for contract-line coverage.");
                if (!string.IsNullOrWhiteSpace(command.VariationOrderQuoteId)) errors.Add("VariationOrderQuoteId must be empty for contract-line coverage.");
                break;
            case BidPackageLineCoverage.Variation:
                if (string.IsNullOrWhiteSpace(command.VariationOrderQuoteId)) errors.Add("VariationOrderQuoteId is required for variation coverage.");
                if (!string.IsNullOrWhiteSpace(command.BoqLineItemId)) errors.Add("BoqLineItemId must be empty for variation coverage.");
                break;
            case BidPackageLineCoverage.Unassigned:
                if (!string.IsNullOrWhiteSpace(command.BoqLineItemId) || !string.IsNullOrWhiteSpace(command.VariationOrderQuoteId))
                    errors.Add("Clearing coverage cannot supply a BoQ line or variation id.");
                break;
            default:
                errors.Add("Unknown coverage value.");
                break;
        }

        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
