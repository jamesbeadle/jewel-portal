using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class RecodeWorkOrderLineValidation
{
    public ValidationOutcome Check(RecodeWorkOrderLine command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.WorkOrderLineId)) errors.Add("WorkOrderLineId is required.");
        if (command.Parts is null || command.Parts.Count == 0)
        {
            errors.Add("At least one part is required.");
            return new ValidationOutcome(errors);
        }
        if (command.Parts.Any(part => string.IsNullOrWhiteSpace(part.CostCode)))
            errors.Add("Every part needs a cost centre.");
        // Zero parts are pointless slices; sign-vs-line and exact-total checks need the
        // line itself, so they live in the handler. (Decimal constants can't appear in
        // patterns, hence the explicit comparison.)
        if (command.Parts.Count > 1 && command.Parts.Any(part => part.Amount == 0m))
            errors.Add("Every part needs a non-zero amount.");
        if (command.Parts.Select(part => part.CostCode).Distinct(StringComparer.OrdinalIgnoreCase).Count() != command.Parts.Count)
            errors.Add("Each cost centre can only appear once — combine the amounts instead.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
