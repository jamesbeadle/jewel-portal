using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SendBidPackageInviteValidation
{
    public ValidationOutcome Check(SendBidPackageInvite command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.BidPackageId)) errors.Add("BidPackageId is required.");
        if (string.IsNullOrWhiteSpace(command.Subject)) errors.Add("Subject is required.");
        else if (command.Subject.Length > 256) errors.Add("Subject must be 256 characters or fewer.");
        if (string.IsNullOrWhiteSpace(command.HtmlBody)) errors.Add("Body is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
