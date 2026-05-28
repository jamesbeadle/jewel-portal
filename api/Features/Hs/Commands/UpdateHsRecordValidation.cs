using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Commands;

public sealed class UpdateHsRecordValidation
{
    public ValidationOutcome Check(UpdateHsRecord command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.HsRecordId)) errors.Add("HsRecordId is required.");
        if (string.IsNullOrWhiteSpace(command.Summary)) errors.Add("Summary is required.");
        if (string.IsNullOrWhiteSpace(command.AssignedToEmail)) errors.Add("Assignee email is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
