using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

public sealed class RestoreAiSkillVersionValidation
{
    private const int FirstVersion = 1;

    public ValidationOutcome Check(RestoreAiSkillVersion command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.SkillKey))
            errors.Add("Name the skill whose version is to be restored.");
        if (command.Version < FirstVersion)
            errors.Add("Name the version to restore — versions are numbered from 1.");

        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
