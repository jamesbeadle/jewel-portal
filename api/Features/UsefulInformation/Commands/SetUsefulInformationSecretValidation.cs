using Jewel.JPMS.Contracts.UsefulInformation;

namespace Jewel.JPMS.Api.Features.UsefulInformation.Commands;

public sealed class SetUsefulInformationSecretValidation
{
    public const int MaximumSecretLength = 256;

    private readonly UsefulInformationOptions options;
    public SetUsefulInformationSecretValidation(UsefulInformationOptions options) { this.options = options; }

    public ValidationOutcome Check(SetUsefulInformationSecret command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.UsefulInformationNoteId)) errors.Add("UsefulInformationNoteId is required.");
        var isHoldingASecret = !string.IsNullOrWhiteSpace(command.Secret);
        if (isHoldingASecret && !options.CanHoldSecrets)
            errors.Add("The portal has no credential key configured, so a credential cannot be held yet.");
        if (isHoldingASecret && command.Secret!.Length > MaximumSecretLength)
            errors.Add($"A credential is at most {MaximumSecretLength} characters.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
