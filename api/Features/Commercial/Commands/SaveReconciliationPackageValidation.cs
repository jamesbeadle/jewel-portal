using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SaveReconciliationPackageValidation
{
    public ValidationOutcome Check(SaveReconciliationPackage command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.Name)) errors.Add("A package name is required.");
        if (command.WorkOrderIds.Count == 0 && command.SalesLines.Count == 0)
            errors.Add("A package needs at least one work order or sales line.");
        if (command.SalesLines.Any(slice => string.IsNullOrWhiteSpace(slice.ValuationLineItemId)))
            errors.Add("Every sales slice needs a valuation line.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
