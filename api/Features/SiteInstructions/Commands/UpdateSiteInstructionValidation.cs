using Jewel.JPMS.Contracts.SiteInstructions;

namespace Jewel.JPMS.Api.Features.SiteInstructions.Commands;

public sealed class UpdateSiteInstructionValidation
{
    public ValidationOutcome Check(UpdateSiteInstruction command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.SiteInstructionId)) errors.Add("SiteInstructionId is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("Title is required.");
        if (string.IsNullOrWhiteSpace(command.Instruction)) errors.Add("Instruction is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
