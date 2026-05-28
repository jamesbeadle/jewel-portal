using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class RecordSiteVisitNotesValidation
{
    public ValidationOutcome Check(RecordSiteVisitNotes command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.SiteVisitId)) errors.Add("SiteVisitId is required.");
        if (command.PhotoCount < 0) errors.Add("Photo count cannot be negative.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
