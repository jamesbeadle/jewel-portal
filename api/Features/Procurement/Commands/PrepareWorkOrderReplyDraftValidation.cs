using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class PrepareWorkOrderReplyDraftValidation
{
    public ValidationOutcome Check(PrepareWorkOrderReplyDraft command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.WorkOrderId)) errors.Add("WorkOrderId is required.");
        if (string.IsNullOrWhiteSpace(command.MailboxMessageId))
            errors.Add("MailboxMessageId is required — the conversation email to reply to.");
        if (string.IsNullOrWhiteSpace(command.HtmlCoverNote))
            errors.Add("HtmlCoverNote is required — it is placed above the quoted history.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
