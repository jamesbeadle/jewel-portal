using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

public sealed class LogTenderEnquiryValidation
{
    public ValidationOutcome Check(LogTenderEnquiry command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        errors.AddRange(TenderEnquiryDetailsRules.Problems(command.Details));
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
