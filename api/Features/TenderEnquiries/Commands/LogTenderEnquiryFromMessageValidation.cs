using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

public sealed class LogTenderEnquiryFromMessageValidation
{
    public ValidationOutcome Check(LogTenderEnquiryFromMessage command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.MessageId)) errors.Add("MessageId is required.");
        var hasProject = !string.IsNullOrWhiteSpace(command.ProjectId);
        var hasDraft = command.NewProject is not null;
        if (!hasProject && !hasDraft) errors.Add("Choose the project the enquiry belongs to, or describe the new one.");
        if (hasProject && hasDraft) errors.Add("An enquiry goes on an existing project OR a new one — not both.");
        if (hasDraft && string.IsNullOrWhiteSpace(command.NewProject!.Name)) errors.Add("The new project needs a name.");
        if (hasDraft && !Enum.IsDefined(command.NewProject!.Organisation)) errors.Add("Choose which Jewel entity the project belongs to.");
        errors.AddRange(TenderEnquiryDetailsRules.Problems(command.Details));
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
