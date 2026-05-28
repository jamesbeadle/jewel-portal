using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Rates;

namespace Jewel.JPMS.Api.Features.Rates.Commands;

public sealed class AddRateValidation
{
    public ValidationOutcome Check(AddRate command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.Trade)) errors.Add("Trade is required.");
        if (string.IsNullOrWhiteSpace(command.Description)) errors.Add("Description is required.");
        if (string.IsNullOrWhiteSpace(command.Unit)) errors.Add("Unit is required.");
        if (command.Value < 0) errors.Add("Value cannot be negative.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
