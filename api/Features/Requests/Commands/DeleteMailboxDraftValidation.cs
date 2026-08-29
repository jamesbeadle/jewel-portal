using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class DeleteMailboxDraftValidation
{
    public ValidationOutcome Check(DeleteMailboxDraft command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.MessageId))
            errors.Add("MessageId is required — the draft's mailbox message id.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
