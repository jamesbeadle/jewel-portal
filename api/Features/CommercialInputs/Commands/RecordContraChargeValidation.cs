using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.CommercialInputs;

namespace Jewel.JPMS.Api.Features.CommercialInputs.Commands;

public sealed class RecordContraChargeValidation
{
    public ValidationOutcome Check(RecordContraCharge command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.SubcontractorReference)) errors.Add("Subcontractor reference is required.");
        if (command.Amount <= 0) errors.Add("Amount must be positive.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
