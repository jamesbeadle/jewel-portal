using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class AgreeSettlementValidation
{
    public ValidationOutcome Check(AgreeSettlement command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (command.FinalContractValue < 0) errors.Add("Final contract value cannot be negative.");
        if (command.FinalCost < 0) errors.Add("Final cost cannot be negative.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
