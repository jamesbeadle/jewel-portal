using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class UpdateWorkOrderValidation
{
    public ValidationOutcome Check(UpdateWorkOrder command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.WorkOrderId)) errors.Add("WorkOrderId is required.");
        if (command.Value <= 0) errors.Add("Value must be positive.");
        if (string.IsNullOrWhiteSpace(command.Scope)) errors.Add("Scope is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
