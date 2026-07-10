using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SetXeroLineWorkOrderLinksValidation
{
    public ValidationOutcome Check(SetXeroLineWorkOrderLinks command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.XeroLedgerLineId)) errors.Add("XeroLedgerLineId is required.");
        if (command.Links is not null && command.Links.Any(link => string.IsNullOrWhiteSpace(link.WorkOrderId)))
            errors.Add("Every split entry needs a work order.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
