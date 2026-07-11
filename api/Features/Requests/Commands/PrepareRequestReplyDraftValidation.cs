using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class PrepareRequestReplyDraftValidation
{
    public ValidationOutcome Check(PrepareRequestReplyDraft command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.RequestId)) errors.Add("RequestId is required.");
        if (string.IsNullOrWhiteSpace(command.MailboxMessageId))
            errors.Add("MailboxMessageId is required — the conversation email to reply to.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
