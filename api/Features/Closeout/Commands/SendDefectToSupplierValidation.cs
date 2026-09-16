using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class SendDefectToSupplierValidation
{
    private const int SubjectMax = 256;

    public ValidationOutcome Check(SendDefectToSupplier command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.DefectId)) errors.Add("DefectId is required.");
        if (command.Subject is { Length: > SubjectMax }) errors.Add($"Subject must be {SubjectMax} characters or fewer.");
        if (command.Subject is not null && string.IsNullOrWhiteSpace(command.Subject)) errors.Add("Subject, when given, must not be blank — omit it to use the portal's wording.");
        if (command.Body is not null && string.IsNullOrWhiteSpace(command.Body)) errors.Add("Body, when given, must not be blank — omit it to use the portal's wording.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
