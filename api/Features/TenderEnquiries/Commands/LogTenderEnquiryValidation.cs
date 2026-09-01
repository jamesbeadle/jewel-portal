using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

public sealed class LogTenderEnquiryValidation
{
    public ValidationOutcome Check(LogTenderEnquiry command)
    {
        var errors = TenderEnquiryProjectChoiceRules.Problems(command.ProjectId, command.NewProject);
        errors.AddRange(TenderEnquiryDetailsRules.Problems(command.Details));
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
