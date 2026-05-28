using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Mobilisation;

namespace Jewel.JPMS.Api.Features.Mobilisation.Commands;

public sealed class UpdateMobilisationChecklistItemValidation
{
    public ValidationOutcome Check(UpdateMobilisationChecklistItem command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.MobilisationItemId)) errors.Add("MobilisationItemId is required.");
        if (string.IsNullOrWhiteSpace(command.Description)) errors.Add("Description is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
