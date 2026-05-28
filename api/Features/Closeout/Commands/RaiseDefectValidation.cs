using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class RaiseDefectValidation
{
    public ValidationOutcome Check(RaiseDefect command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.Description)) errors.Add("Description is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
