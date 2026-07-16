using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SetValuationLineCostCentreValidation
{
    public ValidationOutcome Check(SetValuationLineCostCentre command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ValuationLineItemId)) errors.Add("ValuationLineItemId is required.");
        if (string.IsNullOrWhiteSpace(command.CostCode)) errors.Add("A cost centre is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
