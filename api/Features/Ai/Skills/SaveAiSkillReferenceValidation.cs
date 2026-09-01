using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

public sealed class SaveAiSkillReferenceValidation
{
    private const int MaxBodyLength = 500_000;

    public ValidationOutcome Check(SaveAiSkillReference command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.SkillKey))
            errors.Add("A skill key is required.");
        if (string.IsNullOrWhiteSpace(command.RefKey))
            errors.Add("A reference key is required — lowercase, hyphenated, e.g. jct-clause-map.");
        else if (command.RefKey.Contains(' '))
            errors.Add("Reference keys are hyphenated, not spaced.");
        if (string.IsNullOrWhiteSpace(command.DisplayName))
            errors.Add("A display name is required.");
        if (string.IsNullOrWhiteSpace(command.Body))
            errors.Add("The reference body is empty.");
        else if (command.Body.Length > MaxBodyLength)
            errors.Add($"That reference is too long ({MaxBodyLength:N0} characters max).");

        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
