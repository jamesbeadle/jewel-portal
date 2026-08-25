using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Commands;

public sealed class AddAiAttachmentValidation
{
    // Base64 inflates by 4/3; this bounds the JSON body before any decode happens.
    private const int MaxBase64Length = (AiAttachmentReader.MaxBytes / 3 + 1) * 4;

    private const int MaxFileNameLength = 256;

    public ValidationOutcome Check(AddAiAttachment command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.FileName))
            errors.Add("The file needs a name.");
        else if (command.FileName.Length > MaxFileNameLength)
            // The AiAttachments row's FileName column is nvarchar(256); a longer name would
            // upload the blob and then fail the save, leaving an orphan.
            errors.Add($"That file's name is too long — {MaxFileNameLength} characters at most.");
        else if (!AiAttachmentReader.IsSupported(command.FileName))
            errors.Add($"The assistant can read {AiAttachmentReader.SupportedList} — \"{command.FileName}\" isn't one of those.");

        if (string.IsNullOrWhiteSpace(command.ContentBase64))
            errors.Add("The file uploaded empty.");
        else if (command.ContentBase64.Length > MaxBase64Length)
            errors.Add($"That file is too big — the limit is {AiAttachmentReader.MaxBytes / 1_048_576} MB.");

        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
