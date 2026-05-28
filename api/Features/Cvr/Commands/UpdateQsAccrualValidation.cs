using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Cvr;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class UpdateQsAccrualValidation
{
    public ValidationOutcome Check(UpdateQsAccrual command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.QsAccrualId)) errors.Add("QsAccrualId is required.");
        if (string.IsNullOrWhiteSpace(command.Category)) errors.Add("Category is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
