using System.Linq;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class ReviseVariationOrderLinesValidation
{
    public ValidationOutcome Check(ReviseVariationOrderLines command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.VariationOrderId)) errors.Add("VariationOrderId is required.");
        if (command.Lines is null || command.Lines.Count == 0) errors.Add("At least one line is required.");
        else if (command.Lines.Any(line => string.IsNullOrWhiteSpace(line.CostCode))) errors.Add("Every variation line needs a cost centre.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
