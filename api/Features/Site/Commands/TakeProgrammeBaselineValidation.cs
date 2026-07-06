using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class TakeProgrammeBaselineValidation
{
    public ValidationOutcome Check(TakeProgrammeBaseline command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.Label)) errors.Add("Label is required.");
        if (string.IsNullOrWhiteSpace(command.TakenByEmail)) errors.Add("TakenByEmail is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
