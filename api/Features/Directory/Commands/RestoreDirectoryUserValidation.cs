using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

public sealed class RestoreDirectoryUserValidation
{
    public ValidationOutcome Check(RestoreDirectoryUser command)
    {
        if (string.IsNullOrWhiteSpace(command.Email)) return ValidationOutcome.Failed("Email is required.");
        return ValidationOutcome.Passed;
    }
}
