using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SetClientCostReferencesValidation
{
    // Matches the ClientCostReferences.ClientReference column; the value prints in a 1.3cm
    // PDF column, so anything approaching this is already too long to read.
    public const int MaximumReferenceLength = 64;
    // Matches CostCenters.Code / ClientCostReferences.CostCode.
    public const int MaximumCostCodeLength = 32;

    public ValidationOutcome Check(SetClientCostReferences command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (command.Entries is null) errors.Add("Entries are required (an empty list clears the map).");
        foreach (var entry in command.Entries ?? Array.Empty<ClientCostReferenceEntry>())
        {
            if (string.IsNullOrWhiteSpace(entry.CostCode))
                errors.Add("Every entry needs a cost centre.");
            if ((entry.CostCode ?? "").Trim().Length > MaximumCostCodeLength)
                errors.Add($"'{entry.CostCode}' is longer than a cost centre code can be ({MaximumCostCodeLength} characters).");
            if ((entry.ClientReference ?? "").Trim().Length > MaximumReferenceLength)
                errors.Add($"The client reference for {entry.CostCode} is longer than {MaximumReferenceLength} characters.");
        }
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
