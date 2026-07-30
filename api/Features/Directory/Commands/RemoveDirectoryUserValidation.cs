using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

public sealed class RemoveDirectoryUserValidation
{
    public ValidationOutcome Check(RemoveDirectoryUser command)
    {
        if (string.IsNullOrWhiteSpace(command.Email)) return ValidationOutcome.Failed("Email is required.");
        // The UI already hides Revoke on your own row; this is the server saying the same thing —
        // one misclick (or crafted request) away from locking yourself out of the only screen
        // that could undo it. RevokedBy is stamped from the resolved caller by the endpoint.
        if (string.Equals(command.Email.Trim(), command.RevokedBy.Trim(), StringComparison.OrdinalIgnoreCase))
            return ValidationOutcome.Failed("You can't revoke your own access.");
        return ValidationOutcome.Passed;
    }
}
