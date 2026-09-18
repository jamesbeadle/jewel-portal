using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SendBidPackageInviteValidation
{
    public ValidationOutcome Check(SendBidPackageInvite command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.BidPackageId)) errors.Add("BidPackageId is required.");
        if (string.IsNullOrWhiteSpace(command.Subject)) errors.Add("Subject is required.");
        if (string.IsNullOrWhiteSpace(command.HtmlBody)) errors.Add("The message body is required.");
        // Recipients are checked by the handler, which knows the house BCC-fan-out convention:
        // an empty To is addressed to the projects mailbox itself, so only an entirely empty
        // envelope is wrong, and the message it throws says so.
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
