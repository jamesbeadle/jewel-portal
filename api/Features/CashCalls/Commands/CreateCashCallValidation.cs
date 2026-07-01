using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.CashCalls;

namespace Jewel.JPMS.Api.Features.CashCalls.Commands;

public sealed class CreateCashCallValidation
{
    public ValidationOutcome Check(CreateCashCall command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (command.AmountRequested <= 0) errors.Add("Amount requested must be greater than zero.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
