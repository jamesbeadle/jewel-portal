using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.RecordLinks.Commands;

public sealed class SendProgrammeReplyValidation
{
    public ValidationOutcome Check(SendProgrammeReply command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.MessageId)) errors.Add("messageId is required.");
        if (string.IsNullOrWhiteSpace(command.ReplyBody)) errors.Add("Write the reply before sending it.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
