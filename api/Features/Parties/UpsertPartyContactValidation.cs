using System.Net.Mail;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Parties;

namespace Jewel.JPMS.Api.Features.Parties;

public sealed class UpsertPartyContactValidation
{
    public ValidationOutcome Check(UpsertPartyContact command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.PartyId)) errors.Add("PartyId is required.");
        if (string.IsNullOrWhiteSpace(command.Name)) errors.Add("Name is required.");
        if (string.IsNullOrWhiteSpace(command.Email)) errors.Add("Email is required.");
        else if (!IsValidEmail(command.Email)) errors.Add("Email is not a valid address.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }

    private static bool IsValidEmail(string email)
    {
        try { return new MailAddress(email.Trim()).Address == email.Trim(); }
        catch { return false; }
    }
}
