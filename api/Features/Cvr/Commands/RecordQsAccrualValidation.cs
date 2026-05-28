using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Cvr;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class RecordQsAccrualValidation
{
    public ValidationOutcome Check(RecordQsAccrual command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.Category)) errors.Add("Category is required.");
        if (string.IsNullOrWhiteSpace(command.SignedOffByEmail)) errors.Add("Signed-off email is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
