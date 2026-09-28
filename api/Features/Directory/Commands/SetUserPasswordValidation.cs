using Jewel.JPMS.Contracts.Auth;
using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

/// <summary>The password must meet PasswordPolicy, and it must be someone else's: the page shows
/// no Set password on your own row, and the server says the same thing — setting your own here
/// would end the session you are working in, and your own password is changed by proving the inbox
/// is yours. SetByEmail is stamped from the resolved caller by the endpoint.</summary>
public sealed class SetUserPasswordValidation
{
    public ValidationOutcome Check(SetUserPassword command)
    {
        if (string.IsNullOrWhiteSpace(command.Email)) return ValidationOutcome.Failed("Email is required.");
        var isOwnPassword = string.Equals(
            command.Email.Trim(), command.SetByEmail.Trim(), StringComparison.OrdinalIgnoreCase);
        if (isOwnPassword)
            return ValidationOutcome.Failed("Your own password is changed from the sign-in page — Forgot password emails you a link.");
        var policyRefusal = PasswordPolicy.Validate(command.Password);
        if (policyRefusal is not null) return ValidationOutcome.Failed(policyRefusal);
        return ValidationOutcome.Passed;
    }
}
