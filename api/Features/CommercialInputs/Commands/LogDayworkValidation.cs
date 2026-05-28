using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.CommercialInputs;

namespace Jewel.JPMS.Api.Features.CommercialInputs.Commands;

public sealed class LogDayworkValidation
{
    public ValidationOutcome Check(LogDaywork command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.Description)) errors.Add("Description is required.");
        if (command.Hours < 0) errors.Add("Hours cannot be negative.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
