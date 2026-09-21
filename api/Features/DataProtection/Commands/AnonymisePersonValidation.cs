using Jewel.JPMS.Contracts.DataProtection;

namespace Jewel.JPMS.Api.Features.DataProtection.Commands;

public sealed class AnonymisePersonValidation
{
    private const int ReasonMaxLength = 512;

    public ValidationOutcome Check(AnonymisePerson command)
    {
        var errors = new List<string>();
        var email = command.Email.Trim();
        var isShapedLikeAnEmail = email.Contains('@') && email.Length > 5 && !email.Any(char.IsWhiteSpace);
        if (!isShapedLikeAnEmail) errors.Add("An email address is required.");
        if (PersonPseudonym.IsOne(email)) errors.Add("That address is already a pseudonym — the person has been anonymised.");
        if (string.IsNullOrWhiteSpace(command.Reason)) errors.Add("Say why — the request, or the retention rule — for the audit trail.");
        if (command.Reason.Length > ReasonMaxLength) errors.Add($"The reason must be under {ReasonMaxLength} characters.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
