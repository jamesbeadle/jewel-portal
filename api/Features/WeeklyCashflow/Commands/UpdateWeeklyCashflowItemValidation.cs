using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

public sealed class UpdateWeeklyCashflowItemValidation
{
    public ValidationOutcome Check(UpdateWeeklyCashflowItem command) =>
        WeeklyCashflowItemDetailsRules.Check(command.Details);
}
