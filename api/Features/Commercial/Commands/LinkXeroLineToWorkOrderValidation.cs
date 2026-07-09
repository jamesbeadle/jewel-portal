using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class LinkXeroLineToWorkOrderValidation
{
    public ValidationOutcome Check(LinkXeroLineToWorkOrder command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.XeroLedgerLineId)) errors.Add("XeroLedgerLineId is required.");
        if (command.WorkOrderId is not null && string.IsNullOrWhiteSpace(command.WorkOrderId))
            errors.Add("WorkOrderId must be a work order id or null to unlink.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
