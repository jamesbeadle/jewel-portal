using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Changes;

namespace Jewel.JPMS.Api.Features.Changes.Commands;

public sealed class UpdateChangeDetailsValidation
{
    public ValidationOutcome Check(UpdateChangeDetails command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ChangeRecordId)) errors.Add("ChangeRecordId is required.");
        if (string.IsNullOrWhiteSpace(command.Reference)) errors.Add("Reference is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("Title is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
