using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.CashCalls;

namespace Jewel.JPMS.Api.Features.CashCalls.Commands;

public sealed class IssueClientInvoiceValidation
{
    public ValidationOutcome Check(IssueClientInvoice command)
    {
        if (string.IsNullOrWhiteSpace(command.CashCallId))
            return new ValidationOutcome(new[] { "CashCallId is required." });
        return ValidationOutcome.Passed;
    }
}
