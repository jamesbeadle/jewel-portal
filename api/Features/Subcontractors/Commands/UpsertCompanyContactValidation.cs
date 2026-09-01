using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class UpsertCompanyContactValidation
{
    public ValidationOutcome Check(UpsertCompanyContact command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.SubcontractorId))
            errors.Add("A directory record is required.");
        // A contact needs at least something to reach them by — a name alone is fine (a named
        // person whose details arrive later), but a completely blank row is a mistake.
        if (string.IsNullOrWhiteSpace(command.Name)
            && string.IsNullOrWhiteSpace(command.Email)
            && string.IsNullOrWhiteSpace(command.Phone))
            errors.Add("A contact needs at least a name, an email address or a phone number.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
