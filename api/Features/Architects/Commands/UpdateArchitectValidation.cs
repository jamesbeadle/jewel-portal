using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Architects;

namespace Jewel.JPMS.Api.Features.Architects.Commands;

public sealed class UpdateArchitectValidation
{
    public ValidationOutcome Check(UpdateArchitect command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ArchitectId)) errors.Add("ArchitectId is required.");
        if (string.IsNullOrWhiteSpace(command.Name)) errors.Add("Architect name is required.");
        if (!string.IsNullOrWhiteSpace(command.ContactEmail) && !command.ContactEmail.Contains('@'))
            errors.Add("Contact email is not a valid email address.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
