using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SendValuationStatementEmailValidation
{
    public ValidationOutcome Check(SendValuationStatementEmail command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ValuationClaimId)) errors.Add("ValuationClaimId is required.");
        if (string.IsNullOrWhiteSpace(command.Subject)) errors.Add("Subject is required.");
        if (string.IsNullOrWhiteSpace(command.HtmlBody)) errors.Add("HtmlBody is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
