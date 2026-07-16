using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class RemoveProgrammeTaskValidation
{
    public ValidationOutcome Check(RemoveProgrammeTask command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProgrammeTaskId)) errors.Add("ProgrammeTaskId is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
