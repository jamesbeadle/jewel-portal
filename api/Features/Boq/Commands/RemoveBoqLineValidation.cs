using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Boq;

namespace Jewel.JPMS.Api.Features.Boq.Commands;

public sealed class RemoveBoqLineValidation
{
    public ValidationOutcome Check(RemoveBoqLine command)
    {
        if (string.IsNullOrWhiteSpace(command.BoqLineItemId))
            return ValidationOutcome.Failed("BoqLineItemId is required.");
        return ValidationOutcome.Passed;
    }
}
