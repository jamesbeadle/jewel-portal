using Jewel.JPMS.Contracts.DocumentControl;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

public sealed class SendAttachmentsToDocumentControlValidation
{
    public ValidationOutcome Check(SendAttachmentsToDocumentControl command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.MessageId)) errors.Add("MessageId is required.");
        if (command.AttachmentIds is null || command.AttachmentIds.Count == 0)
            errors.Add("At least one attachment must be ticked.");
        else if (command.AttachmentIds.Any(string.IsNullOrWhiteSpace))
            errors.Add("Every attachment id must be non-empty.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
