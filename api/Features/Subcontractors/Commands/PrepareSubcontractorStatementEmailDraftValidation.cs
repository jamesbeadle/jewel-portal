using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class PrepareSubcontractorStatementEmailDraftValidation
{
    public ValidationOutcome Check(PrepareSubcontractorStatementEmailDraft command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.SubcontractorId)) errors.Add("SubcontractorId is required.");
        if (string.IsNullOrWhiteSpace(command.Subject)) errors.Add("Subject is required.");
        if (string.IsNullOrWhiteSpace(command.HtmlBody)) errors.Add("HtmlBody is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
