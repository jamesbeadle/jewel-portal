using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class SendSubcontractorStatementEmailValidation
{
    public ValidationOutcome Check(SendSubcontractorStatementEmail command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.SubcontractorId)) errors.Add("SubcontractorId is required.");
        if (string.IsNullOrWhiteSpace(command.Subject)) errors.Add("Subject is required.");
        if (string.IsNullOrWhiteSpace(command.HtmlBody)) errors.Add("HtmlBody is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
