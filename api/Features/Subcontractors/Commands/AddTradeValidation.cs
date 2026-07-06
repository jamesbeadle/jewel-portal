using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class AddTradeValidation
{
    public ValidationOutcome Check(AddTrade command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.Name)) errors.Add("Trade name is required.");
        else if (command.Name.Trim().Length > 64) errors.Add("Trade name must be 64 characters or fewer.");
        else if (command.Name.Contains('/')) errors.Add("Trade names are single trades — add each trade separately rather than a slash-separated list.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
