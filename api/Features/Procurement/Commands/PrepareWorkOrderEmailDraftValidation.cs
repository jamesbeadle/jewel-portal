using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class PrepareWorkOrderEmailDraftValidation
{
    public ValidationOutcome Check(PrepareWorkOrderEmailDraft command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.WorkOrderId)) errors.Add("WorkOrderId is required.");
        if (string.IsNullOrWhiteSpace(command.Subject)) errors.Add("Subject is required.");
        if (string.IsNullOrWhiteSpace(command.HtmlBody)) errors.Add("HtmlBody is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
