using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.TenderEnquiries;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

public sealed class SetTenderEnquiryStatusValidation
{
    public ValidationOutcome Check(SetTenderEnquiryStatus command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.TenderEnquiryId)) errors.Add("TenderEnquiryId is required.");
        if (!Enum.IsDefined(command.Status)) errors.Add("Status is not recognised.");
        if (command.Status == TenderEnquiryStatus.Received) errors.Add("An enquiry can't be moved back to Received.");
        var isEnding = !command.Status.IsOpen();
        if (isEnding && string.IsNullOrWhiteSpace(command.Note))
            errors.Add("Say why the enquiry ended this way — the note is the surviving record.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
