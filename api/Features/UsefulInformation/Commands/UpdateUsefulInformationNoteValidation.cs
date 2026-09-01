using Jewel.JPMS.Contracts.UsefulInformation;

namespace Jewel.JPMS.Api.Features.UsefulInformation.Commands;

public sealed class UpdateUsefulInformationNoteValidation
{
    public ValidationOutcome Check(UpdateUsefulInformationNote command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.UsefulInformationNoteId)) errors.Add("UsefulInformationNoteId is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("Title is required.");
        if (string.IsNullOrWhiteSpace(command.Body)) errors.Add("The note itself is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
