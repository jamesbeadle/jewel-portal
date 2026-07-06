using System.Net.Mail;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Projects;

namespace Jewel.JPMS.Api.Features.Projects.Contacts;

public sealed class UpsertProjectContactValidation
{
    public ValidationOutcome Check(UpsertProjectContact command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        // Linked rows (PartyContactId set) take name/email from the party contact; ad-hoc rows
        // must carry their own.
        if (string.IsNullOrWhiteSpace(command.PartyContactId))
        {
            if (string.IsNullOrWhiteSpace(command.Name)) errors.Add("Name is required.");
            if (string.IsNullOrWhiteSpace(command.Email)) errors.Add("Email is required.");
            else if (!IsValidEmail(command.Email)) errors.Add("Email is not a valid address.");
        }
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }

    private static bool IsValidEmail(string email)
    {
        try { return new MailAddress(email.Trim()).Address == email.Trim(); }
        catch { return false; }
    }
}
