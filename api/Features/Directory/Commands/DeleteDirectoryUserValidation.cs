using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

public sealed class DeleteDirectoryUserValidation
{
    public ValidationOutcome Check(DeleteDirectoryUser command)
    {
        if (string.IsNullOrWhiteSpace(command.Email)) return ValidationOutcome.Failed("Email is required.");
        return ValidationOutcome.Passed;
    }
}
