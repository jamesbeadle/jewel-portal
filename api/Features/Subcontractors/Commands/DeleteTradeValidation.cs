using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class DeleteTradeValidation
{
    public ValidationOutcome Check(DeleteTrade command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.TradeId)) errors.Add("TradeId is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
