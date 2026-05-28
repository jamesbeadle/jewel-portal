using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class AgreeVatAnalysisValidation
{
    public ValidationOutcome Check(AgreeVatAnalysis command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (command.ZeroRatedAmount < 0) errors.Add("Zero-rated amount cannot be negative.");
        if (command.StandardRatedAmount < 0) errors.Add("Standard-rated amount cannot be negative.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
