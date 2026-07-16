using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class RemoveProgrammeBaselineValidation
{
    public ValidationOutcome Check(RemoveProgrammeBaseline command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProgrammeBaselineId)) errors.Add("ProgrammeBaselineId is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
