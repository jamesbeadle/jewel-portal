using Jewel.JPMS.Contracts.DocumentControl;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

public sealed class ExtractDocumentControlArchiveValidation
{
    public ValidationOutcome Check(ExtractDocumentControlArchive command)
    {
        if (string.IsNullOrWhiteSpace(command.DocumentControlItemId))
            return new ValidationOutcome(new List<string> { "DocumentControlItemId is required." });
        return ValidationOutcome.Passed;
    }
}
