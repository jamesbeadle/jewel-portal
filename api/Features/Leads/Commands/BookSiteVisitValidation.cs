using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class BookSiteVisitValidation
{
    public ValidationOutcome Check(BookSiteVisit command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.LeadId)) errors.Add("LeadId is required.");
        if (command.ScheduledAt < DateTimeOffset.UtcNow.AddMinutes(-5))
            errors.Add("Scheduled date must be in the future.");
        if (command.AttendeeEmails.Count == 0)
            errors.Add("At least one attendee is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
