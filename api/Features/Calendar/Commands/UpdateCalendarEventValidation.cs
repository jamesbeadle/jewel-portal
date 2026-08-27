using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Calendar;

namespace Jewel.JPMS.Api.Features.Calendar.Commands;

public sealed class UpdateCalendarEventValidation
{
    public ValidationOutcome Check(UpdateCalendarEvent command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.CalendarEventId)) errors.Add("CalendarEventId is required.");
        errors.AddRange(CalendarEventDetailsRules.Problems(command.Details));
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
