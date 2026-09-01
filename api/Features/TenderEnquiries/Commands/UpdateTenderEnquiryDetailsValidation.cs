using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

public sealed class UpdateTenderEnquiryDetailsValidation
{
    public ValidationOutcome Check(UpdateTenderEnquiryDetails command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.TenderEnquiryId)) errors.Add("TenderEnquiryId is required.");
        errors.AddRange(TenderEnquiryDetailsRules.Problems(command.Details));
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
