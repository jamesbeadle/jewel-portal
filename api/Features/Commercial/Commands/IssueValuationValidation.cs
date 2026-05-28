using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class IssueValuationValidation
{
    public ValidationOutcome Check(IssueValuation command)
    {
        if (string.IsNullOrWhiteSpace(command.ValuationId)) return ValidationOutcome.Failed("ValuationId is required.");
        return ValidationOutcome.Passed;
    }
}
