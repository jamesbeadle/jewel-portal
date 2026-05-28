using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class UpdateDefectValidation
{
    public ValidationOutcome Check(UpdateDefect command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.DefectId)) errors.Add("DefectId is required.");
        if (string.IsNullOrWhiteSpace(command.Description)) errors.Add("Description is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
