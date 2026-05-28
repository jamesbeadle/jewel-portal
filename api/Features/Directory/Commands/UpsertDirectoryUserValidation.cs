using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

public sealed class UpsertDirectoryUserValidation
{
    public ValidationOutcome Check(UpsertDirectoryUser command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.Email)) errors.Add("Email is required.");
        if (string.IsNullOrWhiteSpace(command.DisplayName)) errors.Add("Display name is required.");
        if (command.Roles.Count == 0) errors.Add("At least one role is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
