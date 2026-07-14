using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class UpdateManualWorkOrderValidation
{
    public ValidationOutcome Check(UpdateManualWorkOrder command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.WorkOrderId)) errors.Add("WorkOrderId is required.");
        if (string.IsNullOrWhiteSpace(command.SubcontractorId)) errors.Add("SubcontractorId is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("Title is required.");
        if (command.Lines is null || command.Lines.Count == 0)
        {
            errors.Add("At least one priced line is required.");
            return new ValidationOutcome(errors);
        }
        if (command.Lines.Any(line => string.IsNullOrWhiteSpace(line.CostCode)))
            errors.Add("Every line needs a cost centre.");
        if (command.Lines.Any(line => string.IsNullOrWhiteSpace(line.Title)))
            errors.Add("Every line needs a title.");
        // Decimal constants can't appear in patterns, hence the explicit comparison.
        if (command.Lines.Any(line => line.Amount == 0m))
            errors.Add("Every line needs a non-zero amount.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
