using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

public sealed class LogTenderEnquiryFromMessageValidation
{
    public ValidationOutcome Check(LogTenderEnquiryFromMessage command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.MessageId)) errors.Add("MessageId is required.");
        errors.AddRange(TenderEnquiryProjectChoiceRules.Problems(command.ProjectId, command.NewProject));
        errors.AddRange(TenderEnquiryDetailsRules.Problems(command.Details));
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
