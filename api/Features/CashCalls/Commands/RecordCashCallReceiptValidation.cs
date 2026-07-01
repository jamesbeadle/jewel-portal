using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.CashCalls;

namespace Jewel.JPMS.Api.Features.CashCalls.Commands;

public sealed class RecordCashCallReceiptValidation
{
    public ValidationOutcome Check(RecordCashCallReceipt command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.CashCallId)) errors.Add("CashCallId is required.");
        if (command.AmountReceived <= 0) errors.Add("Amount received must be greater than zero.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
