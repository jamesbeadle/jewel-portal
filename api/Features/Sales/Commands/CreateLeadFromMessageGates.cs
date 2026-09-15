using Jewel.JPMS.Api.Features.Requests; // TriageRoles (internal, same assembly)
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Commands;

// The Control Centre's roles, not the sales team's: raising a lead from an email is a triage
// decision taken on the Sales pane, so it is gated exactly as filing any other email is
// (LinkMessageToRecordAuthorisation reads the same set).
public sealed class CreateLeadFromMessageAuthorisation
{
    public bool Allows(SignedInUser user, CreateLeadFromMessage command) =>
        TriageRoles.AllowedToTriage.IncludesAny(user.Roles);
}

public sealed class CreateLeadFromMessageValidation
{
    public ValidationOutcome Check(CreateLeadFromMessage command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.MessageId)) errors.Add("MessageId is required.");
        if (string.IsNullOrWhiteSpace(command.ContactName) && string.IsNullOrWhiteSpace(command.ContactEmail))
            errors.Add("A contact name or a contact email is required.");
        SalesFieldLimits.Check(errors, command.ContactName, 256, "Contact name");
        SalesFieldLimits.Check(errors, command.ContactEmail, 256, "Contact email");
        SalesFieldLimits.Check(errors, command.ContactPhone, 64, "Contact phone");
        SalesFieldLimits.Check(errors, command.CompanyName, 256, "Company name");
        SalesFieldLimits.Check(errors, command.PropertyAddress, 512, "Property address");
        SalesFieldLimits.Check(errors, command.Postcode, 16, "Postcode");
        SalesFieldLimits.Check(errors, command.Summary, 512, "Summary");
        SalesFieldLimits.Check(errors, command.Notes, 4000, "Notes");
        SalesFieldLimits.Check(errors, command.OwnerEmail, 256, "Owner email");
        if (command.EstimatedValue is < 0) errors.Add("Estimated value cannot be negative.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
