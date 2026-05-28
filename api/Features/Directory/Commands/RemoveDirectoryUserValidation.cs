using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

public sealed class RemoveDirectoryUserValidation
{
    public ValidationOutcome Check(RemoveDirectoryUser command)
    {
        if (string.IsNullOrWhiteSpace(command.Email)) return ValidationOutcome.Failed("Email is required.");
        return ValidationOutcome.Passed;
    }
}
