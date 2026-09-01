using Jewel.JPMS.Contracts.UsefulInformation;

namespace Jewel.JPMS.Api.Features.UsefulInformation.Commands;

public sealed class DeleteUsefulInformationNoteValidation
{
    public ValidationOutcome Check(DeleteUsefulInformationNote command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.UsefulInformationNoteId)) errors.Add("UsefulInformationNoteId is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
