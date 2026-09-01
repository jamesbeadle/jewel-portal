using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

public sealed class CreateWeeklyCashflowItemValidation
{
    public ValidationOutcome Check(CreateWeeklyCashflowItem command) =>
        WeeklyCashflowItemDetailsRules.Check(command.Details);
}
