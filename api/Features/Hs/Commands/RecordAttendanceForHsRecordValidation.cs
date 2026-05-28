using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Commands;

public sealed class RecordAttendanceForHsRecordValidation
{
    public ValidationOutcome Check(RecordAttendanceForHsRecord command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.HsRecordId)) errors.Add("HsRecordId is required.");
        if (string.IsNullOrWhiteSpace(command.AttendeeName)) errors.Add("Attendee name is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
