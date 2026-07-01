using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Clients;

namespace Jewel.JPMS.Api.Features.Clients.Commands;

public sealed class CreateClientValidation
{
    public ValidationOutcome Check(CreateClient command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.Name)) errors.Add("Client name is required.");
        if (!string.IsNullOrWhiteSpace(command.ArchitectEmail) && !command.ArchitectEmail.Contains('@'))
            errors.Add("Architect email is not a valid email address.");
        if (!string.IsNullOrWhiteSpace(command.PrimaryContactEmail) && !command.PrimaryContactEmail.Contains('@'))
            errors.Add("Primary contact email is not a valid email address.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
