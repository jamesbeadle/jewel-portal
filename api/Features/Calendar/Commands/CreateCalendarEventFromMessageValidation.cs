using Jewel.JPMS.Contracts.Calendar;

namespace Jewel.JPMS.Api.Features.Calendar.Commands;

public sealed class CreateCalendarEventFromMessageValidation
{
    public ValidationOutcome Check(CreateCalendarEventFromMessage command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.MessageId)) errors.Add("MessageId is required.");
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        errors.AddRange(CalendarEventDetailsRules.Problems(command.Details));
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
